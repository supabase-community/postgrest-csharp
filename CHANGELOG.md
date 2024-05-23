# Changelog

## 4.0.3 - 2024-05-23

- Re: [#97](https://github.com/supabase-community/postgrest-csharp/pull/97) Fix set null value on string property.
  Thanks [@alustrement-bob](https://github.com/alustrement-bob)!

## 4.0.2 - 2024-05-16

- Re: [#96](https://github.com/supabase-community/postgrest-csharp/pull/96) Set `ConfigureAwait(false)` the response to
  prevent deadlocking applications. Thanks [@pur3extreme](https://github.com/pur3extreme)!

## 4.0.1 - 2024-05-07

- Re: [#92](https://github.com/supabase-community/postgrest-csharp/issues/92) Changes `IPostgrestTable<>` contract to
  return the interface rather than a concrete type.

## 4.0.0 - 2024-04-21

- [MAJOR] Moves namespaces from `Postgrest` to `Supabase.Postgrest`
- Re: [#135](https://github.com/supabase-community/supabase-csharp/issues/135) Update nuget package
  name `postgrest-csharp` to `Supabase.Postgrest`

## 3.5.1 - 2024-03-15

- Re: [#147](https://github.com/supabase-community/supabase-csharp/issues/147) - Supports `Rpc` specifying a generic
  type for its return.

## 3.5.0 - 2024-01-14

- Re: [#78](https://github.com/supabase-community/postgrest-csharp/issues/78), Generalize query filtering creation
  in `Table` so that it matches new generic signatures.
- Move from `QueryFilter` parameters to a more generic `IPostgrestQueryFilter` to support constructing new QueryFilters
  from a LINQ expression.
    - Note: Lists of `QueryFilter`s will now need to be defined
      as: `new List<IPostgrestQueryFilter> { new QueryFilter(), ... }`
- Adjust serialization of timestamps within a `QueryFilter` to support `DateTime` and `DateTimeOffset` using the
  ISO-8601 (https://stackoverflow.com/a/115002)

## 3.4.1 - 2024-01-08

- Re: [#85](https://github.com/supabase-community/postgrest-csharp/issues/85) Fixes problem when using multiple .Order()
  methods by merging [#86](https://github.com/supabase-community/postgrest-csharp/pull/86).
  Thanks [@hunsra](https://github.com/hunsra)!

## 3.4.0 - 2024-01-03

- Re: [#81](https://github.com/supabase-community/postgrest-csharp/issues/81)
    - [Minor] Removes `IgnoreOnInsert`and `IgnoreOnUpdate` from `ReferenceAttribute` as changing these properties
      to `false` does not currently provide the expected functionality.
    - Fixes `Insert` and `Update` not working on models that have `Reference` specified on a property with a non-null
      value.

## 3.3.0 - 2023-11-28

- Re: [#78](https://github.com/supabase-community/postgrest-csharp/issues/78) Updates signatures for `Not` and `Filter`
  to include generic types for a better development experience.
- Updates internal generic type names to be more descriptive.
- Add support for LINQ predicates on `Table<TModel>.Not()` signatures

## 3.2.10 - 2023-11-13

- Re: [#76](https://github.com/supabase-community/postgrest-csharp/issues/76) Removes the incorrect `ToUniversalTime`
  conversion in the LINQ `Where` parser.

## 3.2.9 - 2023-10-09

- Re: [supabase-csharp#115](https://github.com/supabase-community/supabase-csharp/discussions/115) Additional support
  for a model referencing another model with multiple foreign keys.

## 3.2.8 - 2023-10-08

- Re: [supabase-csharp#115](https://github.com/supabase-community/supabase-csharp/discussions/115) Adds support for
  multiple references attached to the same model (foreign keys) on a single C# Model.

## 3.2.7 - 2023-09-15

- Implements a `TableWithCache` for `Get` requests that can pull reactive Models from cache before making a remote
  request.
- Re: [supabase-csharp#85](https://github.com/supabase-community/supabase-csharp/issues/85) Includes sourcelink support.

## 3.2.6 - 2023-09-04

- Re: [#75](https://github.com/supabase-community/postgrest-csharp/pull/75) Fix issue with marshalling of stored
  procedure arguments. Big thank you to [@corrideat](https://github.com/corrideat)!

## 3.2.5 - 2023-07-13

- Re: [supabase-community/supabase-csharp#81](https://github.com/supabase-community/supabase-csharp/discussions/81) -
  Clarifies `ReferenceAttribute` by changing `shouldFilterTopLevel` to `useInnerJoin` and adds an additional
  constructor for `ReferenceAttribute` with a shortcut for specifying the `JoinType`

## 3.2.4 - 2023-06-29

- [#70](https://github.com/supabase-community/postgrest-csharp/pull/70) Minor Unity related fixes

## 3.2.3 - 2023-06-25

- [#69](https://github.com/supabase-community/postgrest-csharp/pull/69) Locks language version to C#9
- [#68](https://github.com/supabase-community/postgrest-csharp/pull/68) Makes RPC parameters optional

Thanks [@wiverson](https://github.com/wiverson) for the work in this release!

## 3.2.2 - 2023-06-10

- Uses new assembly name of `Supabase.Core`

## 3.2.1 - 2023-06-10

- Changes Assembly output to be `Supabase.Postgrest`

## 3.2.0 - 2023-05-23

- General codebase and QOL improvements. Exceptions are generally thrown through `PostgrestException` now instead
  of `Exception`. A `FailureHint.Reason` is provided with failures if possible to parse.
- `AddDebugListener` is now available on the client to help with debugging
- Merges [#65](https://github.com/supabase-community/postgrest-csharp/pull/65) Cleanup + Add better exception handling
- Merges [#66](https://github.com/supabase-community/postgrest-csharp/pull/66) Local test Fixes
- Fixes [#67](https://github.com/supabase-community/postgrest-csharp/issues/67) Postgrest Reference attribute is
  producing StackOverflow for circular references

## 3.1.3 - 2023-01-28

- Fix [#61](https://github.com/supabase-community/postgrest-csharp/issues/61) which further typechecks nullable values.

## 3.1.2 - 2023-01-27

- Fix [#61](https://github.com/supabase-community/postgrest-csharp/issues/61) which did not correctly parse Linq `Where`
  when encountering a nullable type.
- Add missing support for transforming for `== null` and `!= null`

## 3.1.1 - 2023-01-17

- Fix issue from supabase-community/supabase-csharp#48 where boolean model properties would not be evaluated in
  predicate expressions

## 3.1.0 - 2023-01-16

- [Minor] Breaking API Change: `PrimaryKey` attribute defaults to `shouldInsert: false` as most uses will have the
  Database generate the primary key.
- Merged [#60](https://github.com/supabase-community/postgrest-csharp/pull/60) which Added linq support
  for `Select`, `Where`, `OnConflict`, `Columns`, `Order`, `Update`, `Set`, and `Delete`

## 3.0.4 - 2022-11-22

## 3.0.3 - 2022-11-22

- `GetHeaders` is now passed to `ModeledResponse` and `BaseModel` so that the default `Update` and `Delete` methods use
  the latest credentials
- `GetHeaders` is used in `Rpc` calls (re: [#39](https://github.com/supabase-community/supabase-csharp/issues/39))

## 3.0.2 - 2022-11-12

- `IPostgrestClient` and `IPostgrestAPI` now implement `IGettableHeaders`

## 3.0.1 - 2022-11-10

- Make `SerializerSettings` publicly accessible.

## 3.0.0 - 2022-11-08

- Re: [#54](https://github.com/supabase-community/postgrest-csharp/pull/54) Restructure Project to support DI and enable
  Nullity
    - `Client` is no longer a singleton class.
    - `StatelessClient` has been removed as `Client` performs the same essential functions.
    - `Table` default constructor requires reference to `JsonSerializerSettings`
    - `BaseModel` now keeps track of `BaseUrl` and `RequestClientOptions`. These are now used in the default (and
      overridable) `BaseModel.Update` and `BaseModel.Delete` methods (as they previously referenced the singleton).
    - All publicly facing classes (that offer functionality) now include an Interface.
    - `RequestException` is no longer thrown for attempting to update a record that does not exist, instead an
      empty `ModeledResponse` is returned.

## 2.1.1 - 2022-10-19

-

Re: [#50](https://github.com/supabase-community/postgrest-csharp/issues/50) & [#51](https://github.com/supabase-community/postgrest-csharp/pull/51)
Adds `shouldFilterTopRows` as constructor parameter for `ReferenceAttribute` which defaults to `true` to match current
API expectations.

## 2.1.0 - 2022-10-11

- [Minor] Breaking API change: Remove `BaseModel.PrimaryKeyValue` and `BaseModel.PrimaryKeyColumn` in favor of
  a `PrimaryKey` dictionary with support for composite keys.
- Re: [#48](https://github.com/supabase-community/postgrest-csharp/issues/48) - Add support for derived models
  on `ReferenceAttribute`
- Re: [#49](https://github.com/supabase-community/postgrest-csharp/issues/49) - Added `Match(T model)`

## 2.0.12 - 2022-09-13

- Merged [#47](https://github.com/supabase-community/postgrest-csharp/pull/47) which added cancellation token support
  to `Table<T>` methods. Thanks [@devpikachu](https://github.com/devpikachu)!

## 2.0.11 - 2022-08-01

- Additional `OnConflict` Access via `QueryOptions` with reference
  to [supabase-community/supabase-csharp#29](https://github.com/supabase-community/supabase-csharp/issues/29)

## 2.0.10 - 2022-08-01

- Added `OnConflict` parameter for UNIQUE resolution with reference
  to [supabase-community/supabase-csharp#29](https://github.com/supabase-community/supabase-csharp/issues/29)

## 2.0.9 - 2022-07-17

- Merged [#44](https://github.com/supabase-community/postgrest-csharp/pull/44) Fixing zero length content when sending
  requests without body. Thanks [@SameerOmar](https://github.com/sameeromar)!

## 2.0.8 - 2022-05-24

- Implements [#41](https://github.com/supabase-community/postgrest-csharp/issues/41), which adds support for `infinity`
  and `-infinity` as readable values.

## 2.0.7 - 2022-04-09

- Merged [#39](https://github.com/supabase-community/postgrest-csharp/pull/39), which a fixed shadowed variable
  in `Table.And` and `Table.Or`. Thanks [@erichards3](https://github.com/erichards3)!

## 2.0.6 - 2021-12-30

- Fix for [#38](https://github.com/supabase-community/postgrest-csharp/issues/38), Add support for `NullValueHandling`
  to be specified on a `Column` Attribute and for it to be honored on Inserts and Updates. Defaults
  to: `NullValueHandling.Include`.

## 2.0.5 - 2021-12-26

- Fix for [#37](https://github.com/supabase-community/postgrest-csharp/issues/37) - Fixes #37 - Return Type `minimal`
  would fail to resolve because of incorrect `Accept` headers. Added header and test to verify for future.

## 2.0.4 - 2021-12-26

- Fix for [#36](https://github.com/supabase-community/postgrest-csharp/issues/36) - Inserting/Upserting bulk records
  would fail while doing an unnecessary generic coercion.

## 2.0.3 - 2021-11-26

- Add a `StatelessClient` static class (re: [#7](https://github.com/supabase-community/supabase-csharp/issues/7)) that
  enables API interactions through specifying `StatelessClientOptions`
- Fix for [#35](https://github.com/supabase-community/postgrest-csharp/issues/35) - Client now handles DateTime[]
  serialization and deserialization.
- Added tests for `StatelessClient`
- Added "Kitchen Sink" tests for roundtrip serialization and deserialization data coersion.
