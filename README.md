<p align="center">
<img width="300" src=".github/postgrest-csharp.png"/>
</p>

---

# postgrest-csharp

![Build And Test](https://github.com/supabase/postgrest-csharp/workflows/Build%20And%20Test/badge.svg)
[![Nuget Release](https://img.shields.io/badge/dynamic/json?color=green&label=Nuget%20Release&query=data[0].version&url=https%3A%2F%2Fazuresearch-usnc.nuget.org%2Fquery%3Fq%3Dpackageid%3Apostgrest-csharp)](https://www.nuget.org/packages/postgrest-csharp/)

Documentation can be found [here](https://supabase.github.io/postgrest-csharp/api/Postgrest.html).

Postgrest-csharp is written primarily as a helper library for [supabase/supabase-csharp](https://github.com/supabase/supabase-csharp), however, it should be easy enough to use outside of the supabase ecosystem.

The bulk of this library is a translation and c-sharp-ification of the [supabase/postgrest-js](https://github.com/supabase/postgrest-js) library.

## Status

- [x] Connects to PostgREST Server
- [x] Authentication
- [x] Basic Query Features
  - [x] CRUD
  - [x] Single
  - [x] Range (to & from)
  - [x] Limit
  - [x] Limit w/ Foreign Key
  - [x] Offset
  - [x] Offset w/ Foreign Key
- [x] Advanced Query Features
  - [x] Filters
  - [x] Ordering
- [ ] Custom Serializers
  - [ ] [Postgres Range](https://www.postgresql.org/docs/9.3/rangetypes.html)
    - [x] `int4range`, `int8range`
    - [ ] `numrange`
    - [ ] `tsrange`, `tstzrange`, `daterange`
- [x] Models
  - [x] `BaseModel` to derive from
  - [x] Coercion of data into Models
- [x] Unit Testing
- [x] Nuget Package and Release

## Package made possible through the efforts of:

| <img src="https://github.com/acupofjose.png" width="150" height="150"> | <img src="https://github.com/elrhomariyounes.png" width="150" height="150"> |
| :----------------------------------------------: | :--------------------------------------------------------: |
|   [acupofjose](https://github.com/acupofjose)    |   [elrhomariyounes](https://github.com/elrhomariyounes)    |

## Contributing

We are more than happy to have contributions! Please submit a PR.
