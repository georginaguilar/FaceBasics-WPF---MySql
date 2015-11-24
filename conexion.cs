using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Microsoft.Samples.Kinect.FaceBasics
{
    public class Conexion
    {
        public static MySqlConnection Conectar()
        {
            MySqlConnection c_conectar = new MySqlConnection("server= 127.0.0.1; database=Conexion; Uid= root; pwd=1234;");
            c_conectar.Open();
            return c_conectar;
        }
    }
}
