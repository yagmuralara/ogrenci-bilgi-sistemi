using System;
using System.Data.SqlClient;

namespace OBSProjesi1
{
    //VeriTabanı bağlantı işlemlerini yöneten sınıf 
    public class DatabaseHelper
    {
        //MSSQL bağlantı cümlesi
        //Server = Local SQL Server 
        //Database = obsVT  kullanılıcak veritabanı
        //Trusted_Connection = True  Windows Authentication kullanılır 
        private string connectionString =
            "Server=LAPTOP-KP1G1MIQ\\SQLEXPRESS;Database=OBS;Trusted_Connection=True;";

        //SQl bağlantısını geri döndüren metot 
        public SqlConnection BaglantiGetir()
        {
            //Yeni bağlantı nesnesi oluşturulur 
            SqlConnection conn =
                new SqlConnection(connectionString);

            //Oluşturulan bağlantı geri döndürülür
            return conn;
        }
    }
}