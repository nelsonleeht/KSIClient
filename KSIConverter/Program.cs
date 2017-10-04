using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace KSIConverter
{
    enum logLevel {Warn, Error, Info};

    class Program
    {
        // Create a logger for use in this class
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            string sHash = string.Empty;
            string sSignature = string.Empty;
            string sFile = string.Empty;

            try
            {
                //# Testing
                //log4net.Config.XmlConfigurator.Configure();
                //log.Debug("Debugging");             //does not get logged
                //log.Info("Entering Application"); //logged to console and log file
                //log.Debug("Debug Statement");     //does not get logged 
                //log.Error("Error statement");     //logged to console and log file
                //log.Warn("Warning statement");    //logged to console and log file
                //log.Fatal("Fatal Statement");     //logged to console and log file

                // Test if input arguments were supplied:
                if (args.Length == 0)
                {                    
                    printLog("Please specify a valid PDF file path.", logLevel.Warn);
                    
                }
                else
                {
                    //sFile = @"C:\Users\Nelson Lee\Desktop\Work\ksi-dev-guide.pdf";
                    sFile = args[0];

                    printLog("sFile>>>" + sFile, logLevel.Info);

                    if (!File.Exists(sFile))
                    {
                        throw new Exception("File doesn't exist.");
                    }

                    //# Convet to hash value
                    sHash = getHash(sFile);
                    
                    //# KSI sign document
                    sSignature = getSignature(sHash);                    

                }
            }
            catch (Exception e)
            {
                Console.Write("Error: " + e.Message);
                log.Error(e);
            }
            finally
            {
                printLog("Done.", logLevel.Info);
            }
        }

        private static string getHash(string filename)
        {
            string strReturn = string.Empty;
            string h = "";

            printLog("Calculating hash value...", logLevel.Info);
            using (System.Security.Cryptography.HashAlgorithm hashAlgorithm = SHA256.Create())
            {
                byte[] plainText = File.ReadAllBytes(filename);
                byte[] hash = hashAlgorithm.ComputeHash(plainText);
                h = Convert.ToBase64String(hash);
            }

            //Console.WriteLine("h: " + h);
            strReturn = h;
            
            return strReturn;
        }

        private static string getSignature(string hashVal)
        {
            string strResult = string.Empty;
            
            string webAddr = ConfigurationSettings.AppSettings["GTAPI"];
            string token = ConfigurationSettings.AppSettings["GTToken"];

            printLog("Getting Signature...", logLevel.Info);
            printLog("Pre-HTTP Web Request : Create : OK", logLevel.Info);

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
            
            printLog("HTTP Web Request : Create : OK", logLevel.Info);
            
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.PreAuthenticate = true;
            httpWebRequest.Headers.Add("Authorization", "Bearer " + token);
            
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {                
                string json = "{\"DataHash\":\"**dh**\", \"Metadata\":\"Test\" }";
                json = json.Replace("**dh**", hashVal);

                //Console.WriteLine(json);
                printLog(json, logLevel.Info);

                streamWriter.Write(json);
                streamWriter.Flush();
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            var sig = "";
            var id = "";
            printLog("Pre-HTTP Web Request POST : Create : OK", logLevel.Info);

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                printLog("POST HTTP Web POST : Create : OK", logLevel.Info);                
                var responseText = streamReader.ReadToEnd();
                var results = JsonConvert.DeserializeObject<dynamic>(responseText);
                printLog("RESULTS : GET : OK", logLevel.Info);
                id = results.Data.id;
                sig = results.Data.signature;
                strResult = sig;

                printLog("Signature ID : " + id, logLevel.Info);
            }
            
            return strResult;
        }        

        static void printLog(string iMsg, logLevel iType, bool printToScreen = true)
        {
            switch (iType)
            {
                case logLevel.Warn:
                    log.Warn(iMsg);
                    break;
                case logLevel.Error:
                    log.Error(iMsg);
                    break;
                default:
                    log.Info(iMsg);
                    break; 
            }                   

            if (printToScreen)
            {
                Console.WriteLine(iMsg);
            }
        }
    }
}
