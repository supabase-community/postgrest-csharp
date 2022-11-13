# Changelog

## 3.0.2 - 2022-11-12

- `IPostgrestClient` and `IPostgrestAPI` now implement `IGettableHeaders`

## 3.0.1 - 2022-11-10

- Make `SerializerSettings` publicly accessible.

## 3.0.0 - 2022-11-08

- Re: [#54](https://github.com/supabase-community/postgrest-csharp/pull/54) Restructure Project to support DI and enable Nullity
	- `Client` is no longer a singleton class.
	- `StatelessClient` has been removed as `Client` performs the same essential functions.
	- `Table` default constructor requires reference to `JsonSerializerSettings`
	- `BaseModel` now keeps track of `BaseUrl` and `RequestClientOptions`. These are now used in the default (and overridable) `BaseModel.Update` and `BaseModel.Delete` methods (as they previously referenced the singleton).
	- All publicly facing classes (that offer functionality) now include an Interface.
	- `RequestException` is no longer thrown for attempting to update a record that does not exist, instead an empty `ModeledResponse` is returned.

## 2.1.1 - 2022-10-19

- Re: [#50](https://github.com/supabase-community/postgrest-csharp/issues/50) & [#51](https://github.com/supabase-community/postgrest-csharp/pull/51) Adds `shouldFilterTopRows` as constructor parameter for `ReferenceAttribute` which defaults to `true` to match current API expectations.

## 2.1.0 - 2022-10-11

- [Minor] Breaking API change: Remove `BaseModel.PrimaryKeyValue` and `BaseModel.PrimaryKeyColumn` in favor of a `PrimaryKey` dictionary with support for composite keys.
- Re: [#48](https://github.com/supabase-community/postgrest-csharp/issues/48) - Add support for derived models on `ReferenceAttribute`
- Re: [#49](https://github.com/supabase-community/postgrest-csharp/issues/49) - Added `Match(T model)`

## 2.0.12 - 2022-09-13

- Merged [#47](https://github.com/supabase-community/postgrest-csharp/pull/47) which added cancellation token support to `Table<T>` methods. Thanks [@devpikachu](https://github.com/devpikachu)!

## 2.0.11 - 2022-08-01

- Additional `OnConflict` Access via `QueryOptions` with reference to [supabase-community/supabase-csharp#29](https://github.com/supabase-community/supabase-csharp/issues/29)

## 2.0.10 - 2022-08-01

- Added `OnConflict` parameter for UNIQUE resolution with reference to [supabase-community/supabase-csharp#29](https://github.com/supabase-community/supabase-csharp/issues/29)

## 2.0.9 - 2022-07-17

- Merged [#44](https://github.com/supabase-community/postgrest-csharp/pull/44) Fixing zero length content when sending requests without body. Thanks [@SameerOmar](https://github.com/sameeromar)!

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
