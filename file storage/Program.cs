using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace http_filetransfer
{
    class Program
    {

        public static string ServerDirectory = "C:/server/";
        public static int BufferSize = 8192;
        public const string DeleteMethod = "DELETE";
        public const int NotFounded = 404;
        public const int BadRequest = 400;
        public const int NotImplemented = 501;

        static void Main(string[] args)
        {
            var httplistener = new HttpListener();
            try
            {
                httplistener.Prefixes.Add("http://*:80/");
                httplistener.Start();
                Console.WriteLine("server successfully runned");
                while (true)
                {
                    IHttpCommand command;
                    HttpListenerContext httplistenercontext = httplistener.GetContext();
                    string commandname = httplistenercontext.Request.HttpMethod;

                    switch (commandname)
                    {
                        case WebRequestMethods.Http.Put:
                            command = new PutCmd();
                            Console.WriteLine("method PUT was called");
                            break;
                        case DeleteMethod:
                            command = new DeleteCmd();
                            Console.WriteLine("method DELETE was called");
                            break;
                        case WebRequestMethods.Http.Head:
                            command = new HeadCmd();
                            Console.WriteLine("method HEAD was called");
                            break;
                        case WebRequestMethods.Http.Get:
                            command = new GetCmd();
                            Console.WriteLine("method GET was called");
                            break;
                        default:
                            Console.WriteLine("Unrecognized Method!");
                            httplistenercontext.Response.StatusCode = NotImplemented;
                            httplistenercontext.Response.OutputStream.Close();
                            continue;
                    }
                    HttpListenerResponse response = httplistenercontext.Response;
                    command.Process(httplistenercontext.Request, ref response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                httplistener.Close();
            }
            Console.ReadLine();
        }




        public class PutCmd : IHttpCommand
        {
            public void Process(HttpListenerRequest request, ref HttpListenerResponse response)
            {
                string fullPath = ServerDirectory + request.RawUrl;
                try
                {
                    string copyHeader = ConfigurationManager.AppSettings["Copy"];
                    var copyPath = request.Headers[copyHeader];
                    if (copyPath != null)
                    {
                        string tempPath = ServerDirectory + "/" + copyPath.TrimStart('/');
                        if (File.Exists(tempPath))
                        {

                            File.Copy(tempPath, fullPath);
                        }
                        else
                        {
                            throw new DirectoryNotFoundException();
                        }
                    }
                    else
                    {
                        var dirname = Path.GetDirectoryName(fullPath);

                        if (!Directory.Exists(dirname))
                        {
                            Directory.CreateDirectory(dirname);
                        }

                        using (var newFile = new FileStream(fullPath, FileMode.OpenOrCreate))
                        {
                            request.InputStream.CopyTo(newFile);

                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    response.StatusCode = NotFounded;
                }
                catch (DirectoryNotFoundException)
                {
                    response.StatusCode = NotFounded;
                }
                catch(Exception ex)
                {
                    response.StatusCode = NotFounded;
                    Console.WriteLine(ex);
                }
                finally
                {
                    response.OutputStream.Close();
                }
            }
        }


        public class DeleteCmd : IHttpCommand
        {

            public void Process(HttpListenerRequest request, ref HttpListenerResponse response)
            {
                try
                {
                    if (Directory.Exists(ServerDirectory + request.RawUrl))
                        Directory.Delete(ServerDirectory + request.RawUrl);
                    else if (File.Exists(ServerDirectory + request.RawUrl))
                        File.Delete(ServerDirectory + request.RawUrl);
                }
                catch (FileNotFoundException)
                {
                    response.StatusCode = NotFounded;
                }
                catch (DirectoryNotFoundException)
                {
                    response.StatusCode = NotFounded;
                }
                catch (Exception)
                {
                    response.StatusCode = BadRequest;
                }
                finally { response.OutputStream.Close(); }
            }
        }



        public class HeadCmd : IHttpCommand
        {

            public void Process(HttpListenerRequest request, ref HttpListenerResponse response)
            {
                string fullPath = ServerDirectory + request.RawUrl;

                try
                {
                    var fileInfo = new FileInfo(fullPath);
                    response.Headers.Add("Name", fileInfo.Name);
                    response.Headers.Add("FileLength", fileInfo.Length.ToString());
                    response.Headers.Add("LastWriteTime", fileInfo.LastWriteTime.ToString("dd/MM/yyyy hh:mm"));
                }
                catch (FileNotFoundException)
                {
                    response.StatusCode = NotFounded;
                }
                catch (DirectoryNotFoundException)
                {
                    response.StatusCode = NotFounded;
                }
                catch (Exception)
                {
                    response.StatusCode = NotFounded;
                }
                finally { response.OutputStream.Close(); }
            }
        }

        public class GetCmd : IHttpCommand
        {

            public void Process(HttpListenerRequest request, ref HttpListenerResponse response)
            {
                Stream output = response.OutputStream;

                var writer = new StreamWriter(output);

                string fullPath = ServerDirectory + request.RawUrl;

                try
                {
                    if (File.Exists(fullPath))
                    {
                        Stream file = new FileStream(fullPath, FileMode.Open);
                        file.CopyTo(output, BufferSize);
                    }
                    else
                    {
                        var directories = Directory.EnumerateFiles(fullPath);

                        foreach (var entry in directories)
                        {
                            writer.Write(JsonConvert.SerializeObject(entry));
                        }
                        writer.Flush();
                    }
                }
                catch (FileNotFoundException)
                {
                    response.StatusCode = NotFounded;
                }
                catch (DirectoryNotFoundException)
                {
                    response.StatusCode = NotFounded;
                }
                catch
                {
                    response.StatusCode = BadRequest;
                }
                finally
                {
                    output.Close();
                    writer.Dispose();
                }
            }
        }



    }
}
