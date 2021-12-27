# Changelog

## 2.0.5 - 2021-12-26

- Fix for [#37](https://github.com/supabase-community/postgrest-csharp/issues/37) - Fixes #37 - Return Type `minimal` would fail to resolve because of incorrect `Accept` headers. Added header and test to verify for future.

## 2.0.4 - 2021-12-26

- Fix for [#36](https://github.com/supabase-community/postgrest-csharp/issues/36) - Inserting/Upserting bulk records would fail while doing an unnecessary generic coercion.

## 2.0.3 - 2021-11-26

- Add a `StatelessClient` static class (re: [#7](https://github.com/supabase-community/supabase-csharp/issues/7)) that enables API interactions through specifying `StatelessClientOptions`
- Fix for [#35](https://github.com/supabase-community/postgrest-csharp/issues/35) - Client now handles DateTime[] serialization and deserialization.
- Added tests for `StatelessClient`
- Added "Kitchen Sink" tests for roundtrip serialization and deserialization data coersion.