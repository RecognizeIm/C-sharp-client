using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            string client_id = "1"; //API User ID
            string clapi_key = "c80fg457eff59f7fff44b9372bafgf2"; //CLAPI user key
            string api_key = "0987ads35a"; //API user key

            //initialize proxy class
            recognize.recognizeProxy proxy = new recognize.recognizeProxy(client_id, api_key , clapi_key);
            
            //insert new image
            Dictionary<string, string> response1 = proxy.imageInsert("CSharp1", "C# sample name", "poznan.jpg");
            
            //apply changes
            Dictionary<string, string> response2 = proxy.indexBuild();
            
            //check progress of applying changes
            Dictionary<string, string> response3 = proxy.indexStatus();

            //recognize image
            Dictionary<string, string> response4 = proxy.recognize("poznan.jpg");
        }
    }
}
