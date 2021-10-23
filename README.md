<p align="center">
<img width="300" src=".github/logo.png"/>
</p>

<p align="center">
  <img src="https://github.com/supabase/postgrest-csharp/workflows/Build%20And%20Test/badge.svg"/>
<a href="https://www.nuget.org/packages/postgrest-csharp/">
  <img src="https://img.shields.io/badge/dynamic/json?color=green&label=Nuget%20Release&query=data[0].version&url=https%3A%2F%2Fazuresearch-usnc.nuget.org%2Fquery%3Fq%3Dpackageid%3Apostgrest-csharp"/>
</a>
</p>

------

### BREAKING CHANGES FOR v2.0.1
- `System.Range` (netstandard2.1) is not available in netstandard2.0, so all `System.Range` calls should be changed to `Postgrest.IntRange` instead.
- `InsertOptions` has been generalized to `QueryOptions` which allows for setting return `minimal` or `representation`
------

Documentation can be found [here](https://supabase-community.github.io/postgrest-csharp/api/Postgrest.html).

Postgrest-csharp is written primarily as a helper library for [supabase/supabase-csharp](https://github.com/supabase/supabase-csharp), however, it should be easy enough to use outside of the supabase ecosystem.

The bulk of this library is a translation and c-sharp-ification of the [supabase/postgrest-js](https://github.com/supabase/postgrest-js) library.

## Getting Started

Postgrest-csharp is _heavily_ dependent on Models deriving from `BaseModel`. To interact with the API, one must have the associated
model specified.

Leverage `Table`,`PrimaryKey`, and `Column` attributes to specify names of classes/properties that are different from their C# Versions.

```c#
[Table("messages")]
public class Message : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("username")]
    public string UserName { get; set; }

    [Column("channel_id")]
    public int ChannelId { get; set; }

    public override bool Equals(object obj)
    {
        return obj is Message message &&
                Id == message.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}
```

Utitilizing the client is then just a matter of instantiating it and specifying the Model one is working with.

```c#
void Initialize()
{
    var client = Client.Initialize("http://localhost:3000");

    // Get All Messages
    var response = await Client.Table<Message>().Get();
    List<Message> models = response.Models;

    // Insert
    var newMessage = new Message { UserName = "acupofjose", ChannelId = 1 };
    await Client.Table<Message>().Insert();

    // Update
    var model = response.Models.First();
    model.UserName = "elrhomariyounes";
    await model.Update();

    // Delete
    await response.Models.Last().Delete();
}
```

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
| :--------------------------------------------------------------------: | :-------------------------------------------------------------------------: |
|              [acupofjose](https://github.com/acupofjose)               |            [elrhomariyounes](https://github.com/elrhomariyounes)            |

## Contributing

We are more than happy to have contributions! Please submit a PR.
