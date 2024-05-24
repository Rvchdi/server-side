using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

namespace TCP_UDP
{
    class ServeurTCP // Renamed the class to ServeurTCP
    {
        private static Thread thread;
        private const int serverPort = 1234;    
        public static MySqlConnection Getconnection()
        {
            string strcnx = @"server=localhost; database=Client_serveur; uid=root; password=;";
            MySqlConnection cnx = new MySqlConnection(strcnx);
            if (cnx != null)
            {
                Console.WriteLine("Connected to the database.");
                return cnx;
            }
            else {
                Console.WriteLine("Not connected.");
                return null;
            }
           
        }

        static void Main(string[] args)
        {
            thread = new Thread(new ThreadStart(Ecoute));
            thread.Start();
            Console.ReadLine();
        }

        public static void Ecoute()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, serverPort);

            try
            {
                listener.Start();
                Console.WriteLine("Server started. Waiting for connections...");
                Getconnection();
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine("Client connected.");
                    HandleClient(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                listener.Stop();
            }
        }

        static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            try
            {            
                byte[] usernameLengthBytes = new byte[sizeof(int)];
                stream.Read(usernameLengthBytes, 0, sizeof(int));
                int usernameLength = BitConverter.ToInt32(usernameLengthBytes, 0);

                byte[] passwordLengthBytes = new byte[sizeof(int)];
                stream.Read(passwordLengthBytes, 0, sizeof(int));
                int passwordLength = BitConverter.ToInt32(passwordLengthBytes, 0);

                byte[] usernameBytes = new byte[usernameLength];
                stream.Read(usernameBytes, 0, usernameLength);
                string username = Encoding.UTF8.GetString(usernameBytes);

                byte[] passwordBytes = new byte[passwordLength];
                stream.Read(passwordBytes, 0, passwordLength);
                string password = Encoding.UTF8.GetString(passwordBytes);

                Console.WriteLine($"Received credentials: Username: {username}, Password: {password}");

                
                MySqlConnection connection = Getconnection();
                connection.Open();

                
                MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM User WHERE username=@username AND password=@password", connection);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                bool isAuthenticated = count > 0;

                
                byte[] responseBytes = BitConverter.GetBytes(isAuthenticated);
                stream.Write(responseBytes, 0, responseBytes.Length);

                Console.WriteLine("Authentication result sent to client.");

               

                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling client: " + ex.Message);
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
    }
}
