# Changelog

## 2.0.10 - 2022-08-01

- Added `OnConflict` parameter for UNIQUE resolution with reference to [supabase-community/supabase-csharp#29](https://github.com/supabase-community/supabase-csharp/issues/29)

## 2.0.9 - 2022-07-17

- Merged [#49](https://github.com/supabase-community/postgrest-csharp/pull/44) Fixing zero length content when sending requests without body. Thanks [@SameerOmar](https://github.com/sameeromar)!

## 2.0.8 - 2022-05-24

- Implements [#41](https://github.com/supabase-community/postgrest-csharp/issues/41), which adds support for `infinity` and `-infinity` as readable values.

## 2.0.7 - 2022-04-09

- Merged [#39](https://github.com/supabase-community/postgrest-csharp/pull/39), which a fixed shadowed variable in `Table.And` and `Table.Or`. Thanks [@erichards3](https://github.com/erichards3)!

## 2.0.6 - 2021-12-30

- Fix for [#38](https://github.com/supabase-community/postgrest-csharp/issues/38), Add support for `NullValueHandling` to be specified on a `Column` Attribute and for it to be honored on Inserts and Updates. Defaults to: `NullValueHandling.Include`.

## 2.0.5 - 2021-12-26

- Fix for [#37](https://github.com/supabase-community/postgrest-csharp/issues/37) - Fixes #37 - Return Type `minimal` would fail to resolve because of incorrect `Accept` headers. Added header and test to verify for future.

## 2.0.4 - 2021-12-26

- Fix for [#36](https://github.com/supabase-community/postgrest-csharp/issues/36) - Inserting/Upserting bulk records would fail while doing an unnecessary generic coercion.

## 2.0.3 - 2021-11-26

- Add a `StatelessClient` static class (re: [#7](https://github.com/supabase-community/supabase-csharp/issues/7)) that enables API interactions through specifying `StatelessClientOptions`
- Fix for [#35](https://github.com/supabase-community/postgrest-csharp/issues/35) - Client now handles DateTime[] serialization and deserialization.
- Added tests for `StatelessClient`
- Added "Kitchen Sink" tests for roundtrip serialization and deserialization data coersion.