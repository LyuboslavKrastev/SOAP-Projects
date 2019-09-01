
using System.ServiceModel;

namespace Shop_App.Services.Interfaces
{
    [ServiceContract]
    interface IService
    {
        [OperationContract]
        string Insert(string Name, int Likes, int Dislikes);
    }
}
