# Plan: remaining LINQ `Where` provider gaps

Follow-up to [PR #122](https://github.com/supabase-community/postgrest-csharp/pull/122) (null-reference crash on captured-value null checks). Four gaps remain in `WhereExpressionVisitor`, ordered below by severity. Each item is its own PR, following the same structure as #122:

- **Tests extracted per LINQ method**: `Where` tests go in `PostgrestTests/LinqWhereTests.cs` (already extracted by #122); any work touching another LINQ method gets its own `Linq<Method>Tests.cs` file.
- **TDD**: every fix starts with a failing test that reproduces the issue (asserting on `GenerateUrl()` — no network needed), then the implementation makes it pass.
- **XML docs**: each PR updates the XML documentation on `IPostgrestTable<TModel>.Where` (and `WhereExpressionVisitor`) to state explicitly which predicate shapes are supported and which are not, with the recommended alternative for unsupported ones.
- Test names follow `Given…_Should…`; no inline comments (extract methods instead); boyscout cleanups in touched code as separate commits.

All fixes reuse the `ParameterFinder` machinery introduced in #122 to decide whether a sub-expression references the model parameter.

---

## 1. High — negation is silently dropped (correctness bug)

**Repro:** `Where(x => !(x.StringValue == "foo"))` generates `?string_value=eq.foo` — the `!` is discarded and the query returns exactly the opposite rows. No exception, no warning. This is the only gap that produces wrong results instead of failing loudly, and it silently mis-scopes `Update`/`Delete` calls filtered this way.

**Cause:** `VisitUnary` is not overridden, so the base visitor walks through the `ExpressionType.Not` node into the inner expression, which sets `Filter` as if the negation were absent.

**Fix:** override `VisitUnary` for `ExpressionType.Not`:
- Visit the operand with a child visitor (same pattern as `VisitBranch`).
- Operand produced a `Filter` → wrap it: `?string_value=not.eq.foo` (PostgREST supports the `not.` prefix on any filter, including `not.and=(…)`/`not.or=(…)` groups — verify `QueryFilter`'s URL generation handles `Operator.Not` around a nested logical filter; if it does not, extend it rather than working around it).
- Operand produced a `ConstantValue` (locally evaluated boolean) → flip it.
- Operand untranslatable → the descriptive `ArgumentException` from #122.

**Failing tests to write first:**
- `GivenNegatedEqualityPredicate_ShouldGenerateNotEqFilter`
- `GivenNegatedNullCheckPredicate_ShouldGenerateNotIsNullFilter`
- `GivenNegatedGroupedPredicate_ShouldGenerateNotWrappedLogicalFilter` (`!(a == 1 || b == 2)`)
- `GivenNegatedStringContainsPredicate_ShouldGenerateNotLikeFilter`

---

## 2. Medium — closure-list `Contains` crashes instead of translating to `in`

**Repro:** `var ids = new List<string> { … }; Where(x => ids.Contains(x.StringValue!))` throws `InvalidOperationException: variable 'x' … referenced from scope '', but it is not defined`. The array/extension form `new[] { 1, 2 }.Contains(x.IntValue!.Value)` fails differently (`Calling context '' is expected to be a member of BaseModel`). This is the standard LINQ idiom for a SQL `IN` query — the most common expectation gap for developers coming from EF Core — and the current error gives no hint that `.Filter(x => x.Col, Operator.In, ids)` is the workaround.

**Cause:** `VisitMethodCall` assumes `Contains` is always called *on a model column* (`x.ListOfStrings.Contains("set")` → `cs` operator). A captured collection inverts that: the column is the *argument*, and compiling it as a standalone lambda crashes on the unbound parameter.

**Fix:** in `VisitMethodCall`, use `ParameterFinder` to classify the call:
- Receiver references the parameter (current behavior) → keep `cs`/`like` translation unchanged.
- Receiver is parameter-free and the argument references the parameter → resolve the column from the argument, evaluate the collection locally, emit `QueryFilter(column, Operator.In, values)`.
- Handle both instance `ICollection.Contains` and static `Enumerable.Contains` (where the collection is `Arguments[0]` and the item is `Arguments[1]`, `node.Object` is null — remove the unconditional null-receiver throw).
- Neither side references the parameter, or both do → descriptive `ArgumentException`.

**Failing tests to write first:**
- `GivenCapturedListContainsColumn_ShouldGenerateInFilter`
- `GivenCapturedArrayContainsColumn_ShouldGenerateInFilter` (extension-method form)
- `GivenColumnListContainsConstant_ShouldStillGenerateContainsFilter` (regression guard for `cs`)
- `GivenColumnStringContainsConstant_ShouldStillGenerateLikeFilter` (regression guard for `like`)

---

## 3. Medium-low — bare boolean member columns are rejected

**Repro:** `Where(x => x.BooleanValue)` and `Where(x => !x.BooleanValue)` throw `Unable to parse the supplied predicate…`. Loud failure with a trivial workaround (`== true`), but idiomatic C# and works everywhere else LINQ is accepted.

**Cause:** the visitor only builds filters from binary expressions and method calls; a lambda body that is a bare `MemberExpression` never sets `Filter`.

**Fix:** where a boolean expression is expected (lambda body and each branch of `VisitBranch`), a `MemberExpression` of type `bool` that references the parameter translates to `QueryFilter(column, Operator.Equals, true)`. The negated form comes free once item 1 lands (`Not` wrapping) — sequence this PR after it.

**Failing tests to write first:**
- `GivenBareBooleanMemberPredicate_ShouldGenerateEqTrueFilter`
- `GivenNegatedBooleanMemberPredicate_ShouldGenerateNotEqTrueFilter`
- `GivenBooleanMemberInsideAndPredicate_ShouldGenerateNestedFilter` (`x.BooleanValue && x.IntValue > 3`)

---

## 4. Low — column-vs-column comparison crashes with a cryptic error

**Repro:** `Where(x => x.DateTimeValue < x.DateTimeValue1)` throws the same cryptic `variable 'x' … not defined` crash.

**Constraint:** this can never be translated — PostgREST's filter syntax has no column-to-column comparison (`?a=gt.b` treats `b` as a literal). The only correct fix is a descriptive error.

**Fix:** in the right-hand-side handling of `VisitBinary` (`HandleMemberExpression` / `HandleUnaryExpression` paths), detect via `ParameterFinder` that the right side references the model parameter and throw an `ArgumentException` explaining that PostgREST cannot compare two columns, suggesting a computed/generated column or an RPC instead.

**Failing test to write first:**
- `GivenColumnComparedToColumnPredicate_ShouldThrowDescriptiveArgumentException` (asserts on the message, mirroring the #122 exception tests)

---

## Verification (every PR)

- `dotnet test` — unit-level assertions run on `GenerateUrl()` without a network.
- Integration pass against local Supabase (`supabase start`, apply the public-schema GRANT workaround); the known pre-existing coercion test failure is unrelated and expected.
