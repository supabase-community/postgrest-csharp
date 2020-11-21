using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Responses;

namespace Postgrest.Models
{
    /// <summary>
    /// Abstract class that must be implemented by C# Postgrest Models.
    /// </summary>
    public abstract class BaseModel
    {
        [Column("status")]
        public string Status { get; set; }

        [Column("inserted_at")]
        public DateTime InsertedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public virtual Task<ModeledResponse<T>> Update<T>() where T : BaseModel, new() => Client.Instance.Builder<T>().Update((T)this);
        public virtual Task Delete<T>() where T : BaseModel, new() => Client.Instance.Builder<T>().Delete((T)this);

        /// <summary>
        /// Gets the value of the PrimaryKey from a model's instance as defined by the [PrimaryKey] attribute on a property on the model.
        /// </summary>
        [JsonIgnore]
        public object PrimaryKeyValue
        {
            get
            {
                var props = this.GetType().GetProperties();
                foreach (var prop in props)
                {
                    var hasAttr = Attribute.GetCustomAttribute(prop, typeof(PrimaryKeyAttribute));
                    if (hasAttr is PrimaryKeyAttribute pka)
                    {
                        return prop.GetValue(this);
                    }
                }

                throw new Exception("Models must specify their Primary Key via the [PrimaryKey] Attribute");
            }
        }

        /// <summary>
        /// Gets the name of the PrimaryKey column on a model's instance as defined by the [PrimaryKey] attribute on a property on the model.
        /// </summary>
        [JsonIgnore]
        public string PrimaryKeyColumn
        {
            get
            {
                var props = this.GetType().GetProperties();
                foreach (var prop in props)
                {
                    var hasAttr = Attribute.GetCustomAttribute(prop, typeof(PrimaryKeyAttribute));
                    if (hasAttr is PrimaryKeyAttribute pka)
                    {
                        return pka.ColumnName;
                    }
                }

                throw new Exception("Models must specify their Primary Key via the [PrimaryKey] Attribute");
            }
        }
    }
}
