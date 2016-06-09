﻿using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.Security;
using NHapiTools.Base.Util;
using NHapi.Base.Model;
using NHapi.Base.Parser;

namespace NHapiTools.Base.Net
{
   /// <summary>
   /// A implementation fo easy use: sending HL7 messages over a TCP/IP + MLLP connection
   /// and receiving a reply
   /// </summary>
   public class SimpleMLLPClientThatTimesOut : IDisposable
   {
      #region Private properties
      private string serverHostname;
      private TcpClient tcpClient;
      private X509CertificateCollection cCollection;
      private NetworkStream clientStream;
      private Stream streamToUse;
      #endregion

      /// <summary>
      /// Defaults to 10 Seconds
      /// </summary>
      public int SendTimeoutInSeconds
      {
         set { tcpClient.SendTimeout = value*1000; }
      }

      /// <summary>
      /// Defaults to 10 Seconds
      /// </summary>
      public int ReceiveTimeoutInSeconds { get; set; }

      #region Constructor
      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="hostname">Hostname to connect to.</param>
      /// <param name="port">Port</param>
      public SimpleMLLPClientThatTimesOut(string hostname, int port)
      {
         serverHostname = hostname;
         cCollection = new X509CertificateCollection();
         tcpClient = new TcpClient(hostname, port);
         clientStream = tcpClient.GetStream();
         streamToUse = clientStream;

         SendTimeoutInSeconds = 10;
         ReceiveTimeoutInSeconds = 10; // default to 10 Seconds
      }
      #endregion

      #region Public methods
      /// <summary>
      /// Use SSL to encrypt the communication
      /// </summary>
      public void EnableSsl()
      {
         SslStream sslStream = new SslStream(clientStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
         if (cCollection.Count > 0)
         {
            // A client side certificate was added, authenticate with certificate
            sslStream.AuthenticateAsClient(serverHostname, cCollection, System.Security.Authentication.SslProtocols.Default, true);
         }
         else
            sslStream.AuthenticateAsClient(serverHostname);

         streamToUse = sslStream;
      }

      /// <summary>
      /// Adds a client side certificate. The certificate is used for client side authentication
      /// </summary>
      /// <param name="pathToCertificate">Path to the local .pfx file</param>
      /// <param name="password">Password of the certificate</param>
      public void AddCertificate(string pathToCertificate, string password)
      {
         cCollection.Add(new X509Certificate(pathToCertificate, password));
      }

      /// <summary>
      /// Send a HL7 message
      /// </summary>
      /// <param name="message">Message to send</param>
      /// <returns>Reply message</returns>
      public IMessage SendHL7Message(IMessage message)
      {
         PipeParser parser = new PipeParser();
         string msg = parser.Encode(message);

         string reply = SendHL7Message(msg);
         return parser.Parse(reply);
      }

      /// <summary>
      /// Send a HL7 message
      /// </summary>
      /// <param name="message">Message to send</param>
      /// <returns>Reply message</returns>
      public string SendHL7Message(string message)
      {
         message = MLLP.CreateMLLPMessage(message);

         // Send the message
         StreamWriter sw = new StreamWriter(streamToUse);
         sw.Write(message);
         sw.Flush();

         var stopwatch = new Stopwatch();
         if (ReceiveTimeoutInSeconds > 0)
         {
            stopwatch.Start();
         }

         // Read the reply
         StringBuilder sb = new StringBuilder();
         bool messageComplete = false;
         while (!messageComplete)
         {
            int b = streamToUse.ReadByte();
            if (b != -1)
               sb.Append((char)b);

            messageComplete = MLLP.ValidateMLLPMessage(sb);

            if (ReceiveTimeoutInSeconds > 0)
            {
               if (stopwatch.ElapsedMilliseconds / 1000 > ReceiveTimeoutInSeconds)
               {
                  messageComplete = true;
                  stopwatch.Stop();
                  return string.Format("Timed out waiting for response after {0} seconds", ReceiveTimeoutInSeconds);
               }
            }
         }
         MLLP.StripMLLPContainer(sb);

         return sb.ToString();
      }

      /// <summary>
      /// Disconnect from the server
      /// </summary>
      public void Disconnect()
      {
         tcpClient.Close();
         streamToUse.Dispose();
      }

      /// <summary>
      /// Disposes the connection
      /// </summary>
      public void Dispose()
      {
         try
         {
            Disconnect();
         }
         catch { }
      }
      #endregion

      #region Private methods
      /// <summary>
      /// The following method is invoked by the RemoteCertificateValidationDelegate.
      /// This allows you to check the certificate and accept or reject it
      /// return true will accept the certificate
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="certificate"></param>
      /// <param name="chain"></param>
      /// <param name="sslPolicyErrors"></param>
      /// <returns></returns>
      private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
      {
         // Accept all certificates
         return true;
      }
      #endregion
   }
}