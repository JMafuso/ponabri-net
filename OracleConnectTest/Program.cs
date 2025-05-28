// Precisa adicionar o pacote NuGet Oracle.ManagedDataAccess.Core
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data; // Adicionado para System.Data.ConnectionState

class Program
{
    static void Main(string[] args)
    {
        // Tente ambas as strings de conexão, uma de cada vez, comentando a outra.
        // String de conexão formato Easy Connect (SID)
        string connectionString = "User Id=rm552570;Password=020596;Data Source=oracle.fiap.com.br:1521/ORCL;";
        
        // String de conexão formato longo (Service Name)
        // string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.fiap.com.br)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));User Id=rm552570;Password=020596;";

        Console.WriteLine($"Tentando conectar com: {connectionString}");

        OracleConnection? con = null;
        try
        {
            con = new OracleConnection(connectionString);
            Console.WriteLine("Objeto OracleConnection criado.");
            Console.WriteLine("Tentando abrir a conexão...");
            con.Open();
            Console.WriteLine("Conexão bem-sucedida!");
            Console.WriteLine($"Versão do Servidor Oracle: {con.ServerVersion}");
            Console.WriteLine($"Estado da Conexão: {con.State}");
        }
        catch (OracleException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nErro OracleException ao conectar:");
            Console.WriteLine($"Número do Erro: {ex.Number}");
            Console.WriteLine($"Mensagem: {ex.Message}");
            Console.WriteLine($"Origem do Erro: {ex.Source}");
            Console.WriteLine("--- Stack Trace ---");
            Console.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                Console.WriteLine("\n--- Inner Exception ---");
                Console.WriteLine(ex.InnerException.ToString());
            }
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nErro geral ao conectar: {ex.GetType().FullName}");
            Console.WriteLine($"Mensagem: {ex.Message}");
            Console.WriteLine("--- Stack Trace ---");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
        }
        finally
        {
            if (con != null)
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                    Console.WriteLine("\nConexão fechada.");
                }
                con.Dispose();
                Console.WriteLine("Objeto OracleConnection disposed.");
            }
        }
        Console.WriteLine("\nPressione qualquer tecla para sair.");
        Console.ReadKey();
    }
}
