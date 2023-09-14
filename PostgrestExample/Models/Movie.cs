using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;

namespace PostgrestExample.Models
{
    [Table("movie")]
    public class Movie : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Reference(typeof(Person))]
        public List<Person> Persons { get; set; }


        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    [Table("person")]
    public class Person : BaseModel
    {
        [PrimaryKey("id",false)]
        public int Id { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Reference(typeof(Profile))]
        public Profile Profile { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    [Table("profile")]
    public class Profile : BaseModel
    {
        [Column("email")]
        public string Email { get; set; }
    }

    [Table("movie_person")]
    public class MoviePerson : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [PrimaryKey("movie_id", false)]
        public int MovieId { get; set; }

        [PrimaryKey("person_id", false)]
        public int PersonId { get; set; }
    }
}
