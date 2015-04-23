﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OmrMarkEngine.Template.Scripting.Util
{
    /// <summary>
    /// Represents a utility for calling rest functions and interpreting the results
    /// </summary>
    public class RestUtil
    {

        /// <summary>
        /// The base uri
        /// </summary>
        private Uri m_baseUri;

        // Trusted certs
        private static List<String> s_trustedCerts = new List<String>();

        /// <summary>
        /// Creates a new instance of the rest utility
        /// </summary>
        public RestUtil(Uri baseUri)
        {
            ServicePointManager.ServerCertificateValidationCallback = RestCertificateValidation;
            this.m_baseUri = baseUri;
        }

        /// <summary>
        /// Validate the REST certificate
        /// </summary>
        static bool RestCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors error)
        {
            if (certificate == null || chain == null)
                return false;
            else
            {
                var valid = s_trustedCerts.Contains(certificate.Subject);
                if(!valid && (chain.ChainStatus.Length > 0 || error != SslPolicyErrors.None))
                    if(MessageBox.Show(String.Format("The remote certificate is not trusted. The error was {0}. The certificate is: \r\n{1}\r\nWould you like to temporarily trust this certificate?", error, certificate.Subject), "Certificate Error", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                        return false;
                    else
                        s_trustedCerts.Add(certificate.Subject);

                return true;
                //isValid &= chain.ChainStatus.Length == 0;
            }


        }

        /// <summary>
        /// Get the specified resource contents
        /// </summary>
        public T Post<T>(String resourcePath, params KeyValuePair<String, Object>[] parms)
        {

            try
            {
                Uri requestUri = new Uri(String.Format("{0}/{1}", this.m_baseUri, resourcePath));
                WebRequest request = WebRequest.Create(requestUri);
                request.Method = "POST";
                request.ContentType = "application/json";
                using(StreamWriter sw = new StreamWriter(request.GetRequestStream()))
                {
                    sw.WriteLine("{");
                    foreach (var itm in parms)
                        sw.WriteLine("\t{0} : '{1}', ", itm.Key, itm.Value);
                    sw.WriteLine("\t\"isActive\" : true");
                    sw.WriteLine("}");
                }
                //serializer.WriteObject(request.GetRequestStream(), data);
                var response = request.GetResponse();

                var serializer = new DataContractJsonSerializer(typeof(T));
                T retVal = (T)serializer.ReadObject(response.GetResponseStream());
                return retVal;
            }
            catch (Exception e)
            {
                throw;
            }

        }
        /// <summary>
        /// Get the specified resource contents
        /// </summary>
        public T Get<T>(String resourcePath, params KeyValuePair<String,Object>[] queryParms)
        {

            try
            {
                StringBuilder queryBuilder = new StringBuilder();
                if(queryParms != null)
                    foreach(var qp in queryParms)
                        queryBuilder.AppendFormat("{0}={1}&", qp.Key, qp.Value);
                if(queryBuilder.Length > 0)
                    queryBuilder.Remove(queryBuilder.Length - 1, 1);
                Uri requestUri = new Uri(String.Format("{0}/{1}?{2}", this.m_baseUri, resourcePath, queryBuilder));

                WebRequest request = WebRequest.Create(requestUri);
                request.Timeout = 600000;
                request.Method = "GET";
                var response = request.GetResponse();
                
                var serializer = new DataContractJsonSerializer(typeof(T));
                T retVal = (T)serializer.ReadObject(response.GetResponseStream());
                //Thread.Sleep(100); // Yeah, you're reading that right... Idk why but GIIS WS don't like to be called too quickly
                return retVal;
            }
            catch(Exception e)
            {
                throw;
            }

        }

        /// <summary>
        /// Get raw unparsed response
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="queryParms"></param>
        /// <returns></returns>
        public string GetRawResponse(String resourcePath, params KeyValuePair<String, Object>[] queryParms)
        {
            try
            {
                StringBuilder queryBuilder = new StringBuilder();
                if (queryParms != null)
                    foreach (var qp in queryParms)
                        queryBuilder.AppendFormat("{0}={1}&", qp.Key, qp.Value);
                if (queryBuilder.Length > 0)
                    queryBuilder.Remove(queryBuilder.Length - 1, 1);
                Uri requestUri = new Uri(String.Format("{0}/{1}?{2}", this.m_baseUri, resourcePath, queryBuilder));

                WebRequest request = WebRequest.Create(requestUri);
                request.Timeout = 600000;
                request.Method = "GET";
                var response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                return reader.ReadToEnd();
            }
            catch (Exception e)
            {
                throw;
            }
        }

    }
}
