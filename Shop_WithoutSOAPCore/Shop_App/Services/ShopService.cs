using Shop_App.Services.Interfaces;
using System.Data;
using System.Data.SqlClient;

namespace Shop_App.Services
{
    public class ShopService : IService
    {
        private const string CONNECTIONSTRING = "Server=localhost\\SQLEXPRESS;Database=Shop;Integrated Security=True";
        private const string INSERT_PROCEDURE_NAME = "dbo.usp_InsertProduct";

        public string Insert(string Name, int Likes, int Dislikes)
        {
            CallInsertProcedure(Name, Likes, Dislikes);

            return "Successfuly inserted product";
        }

        private static void CallInsertProcedure(string Name, int Likes, int Dislikes)
        {
            using (SqlConnection sqlConnection = new SqlConnection(CONNECTIONSTRING))
            {
                using (SqlCommand command = new SqlCommand(INSERT_PROCEDURE_NAME, sqlConnection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("@productName", SqlDbType.VarChar).Value = Name;
                    command.Parameters.Add("@likes", SqlDbType.Int).Value = Likes;
                    command.Parameters.Add("@dislikes", SqlDbType.Int).Value = Dislikes;

                    sqlConnection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
