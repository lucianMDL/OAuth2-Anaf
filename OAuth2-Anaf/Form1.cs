using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace OAuth2_Anaf
{
    public partial class Form1 : Form
    {
        string code = "";
        string URL_auth = "https://logincert.anaf.ro/anaf-oauth2/v1/authorize";
        string URL_getToken = "https://logincert.anaf.ro/anaf-oauth2/v1/token";

        
        Token token = null;

        public Form1()
        {
            InitializeComponent();
        }

        

        private void Form1_Load(object sender, EventArgs e)
        {
      

          
        }

  

        private void button2_Click(object sender, EventArgs e)
        {
            // preia token
            this.Validate();
            if (String.IsNullOrEmpty(edClientID.Text))
                throw new Exception("Lipseste ClientID");
            if (String.IsNullOrEmpty(edSecret.Text))
                throw new Exception("Lipseste SecretID");
            string u = HttpUtility.UrlEncode(edCallBack.Text);
            string url = string.Format("{0}?response_type=code&client_id={1}&redirect_uri={2}",
    URL_auth,
    edClientID.Text,
    u);
            



            var handler = new WebRequestHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            var cert = getCert();
            handler.ClientCertificates.Add(cert);
            handler.UseProxy = false;
            HttpClient client = new HttpClient(handler);

            var resultString = "";
            HttpResponseMessage result = null;
            try
            {
                result = client.GetAsync(url).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            if (result.RequestMessage.RequestUri.Query.Contains("code="))
            {
                code = result.RequestMessage.RequestUri.Query.Replace("?code=", "");
                var par = string.Format("grant_type=authorization_code&code={0}&client_id={1}&client_secret={2}&redirect_uri={3}",
code,
edClientID.Text,
edSecret.Text,
u);
                try
                {
                    result = client.PostAsync(URL_getToken, new StringContent(par)).GetAwaiter().GetResult();
                    resultString = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (resultString.Contains("access_token"))
                    {
                        var js = new JavaScriptSerializer();
                        token = js.Deserialize<Token>(resultString);
                        edB.Text = token.access_token;
                        MessageBox.Show("Tokenul a fost preluat.");

                    }
                    else
                    {
                        MessageBox.Show("Tokenul NU a fost preluat. Rezultat primit: "+resultString);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
                

   



        }
        public static X509Certificate2 getCert(bool toate = false)
        {
            X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
            X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

            X509Certificate2Collection HardCollection = new X509Certificate2Collection();
            string sn = "";


            foreach (var x5 in fcollection)
            {
                if ((toate) || ((x5.IssuerName.Name.ToUpper().Contains("CERTSIGN")) || (x5.IssuerName.Name.ToUpper().Contains("DIGISIGN")) || (x5.IssuerName.Name.ToUpper().Contains("TRAN")) || (x5.IssuerName.Name.ToUpper().Contains("ALFASIGN")) || (x5.IssuerName.Name.ToUpper().Contains("CERT DIGI")) || (x5.IssuerName.Name.ToUpper().Contains("CERTDIGITAL")) || (x5.IssuerName.Name.ToUpper().Contains("DE CALCUL"))))
                {
                    if ((String.IsNullOrEmpty(sn) || (x5.SerialNumber == sn)))
                        HardCollection.Add(x5);
                }
            }
            if (HardCollection.Count > 1)
            {
                X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(HardCollection, "Alege certificatul", "Select a certificate from the following list to get information on that certificate", X509SelectionFlag.SingleSelection);
                store.Close();
                return scollection[0];
            }
            else
            {
                if (HardCollection.Count == 1)
                {
                    store.Close();
                    return HardCollection[0];
                }
                else
                {
                    store.Close();
                    return null;
                }

            }



        }

       

        private void button1_Click(object sender, EventArgs e)
        {
            // preia facturi
            this.Validate();
            if (String.IsNullOrEmpty(edCUI.Text))
            {
                throw new Exception("Lipseste codul fiscal");
            }

            if (String.IsNullOrEmpty(edB.Text))
            {
                throw new Exception("Lipseste tokenul");
            }
            string URL_getFacturi = "https://api.anaf.ro/prod/FCTEL/rest/listaMesajeFactura?zile=60&cif="+edCUI.Text;
            var handler = new WebRequestHandler();

            handler.UseProxy = false;
            HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", edB.Text);

            var ListaFacturiJSON = "";
            HttpResponseMessage result = null;
            try
            {
                result = client.GetAsync(URL_getFacturi).GetAwaiter().GetResult();
                ListaFacturiJSON = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (result.StatusCode != HttpStatusCode.OK)
                    textBox1.Text = result.ReasonPhrase;
                else
                    textBox1.Text = ListaFacturiJSON;

            }
            catch (Exception ex)
            {
                throw new Exception("Ceva a mers naspa :) "+ex.Message);
            }
        }
    }

        
}
