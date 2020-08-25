<p align="center">
<img width="300" src=".github/postgrest-csharp.png"/>
</p>

---

# postgrest-csharp (**VERY MUCH A WIP**)

## This repo is currently public for the sake of contributions - it should NOT be used in anything remotely resembling production

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
- [ ] Advanced Query Features [**In progress**]
  - [ ] Filters
  - [ ] Ordering
- [ ] Coercion Models (using Generics) [**In Progress**]
- [ ] ORM Features
- [ ] **(In Progress)** Unit/Integration Testing
- [ ] Nuget Package and Release

## Contributing

We are more than happy to have contributions! Please submit a PR.
