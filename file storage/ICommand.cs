using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace http_filetransfer
{
    interface IHttpCommand
    {
        void Process(HttpListenerRequest request,ref HttpListenerResponse response);
    }
}
