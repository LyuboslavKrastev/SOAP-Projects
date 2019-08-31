using System.Runtime.Serialization;

namespace Shop_App.Models
{
    [DataContract]
    public class Product
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int Likes { get; set; }

        [DataMember]
        public int Dislikes { get; set; }
    }
}
