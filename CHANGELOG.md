# Changelog

## 2.0.3 - 2021-11-26

- Add a `StatelessClient` static class (re: [#7](https://github.com/supabase-community/supabase-csharp/issues/7)) that enables API interactions through specifying `StatelessClientOptions`
- Fix for [#35](https://github.com/supabase-community/postgrest-csharp/issues/35) - Client now handles DateTime[] serialization and deserialization.
- Added tests for `StatelessClient`
- Added "Kitchen Sink" tests for roundtrip serialization and deserialization data coersion.