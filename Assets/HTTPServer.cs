/*
MIT License

Copyright (c) 2020 gpsnmeajp

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class HTTPServer : MonoBehaviour
{
    HttpListener listener;
    Thread thread = null;
    public string adr = "http://127.0.0.1:8000/";
    public string responseBody = "{}";

    public string ApplicationDataPath;

    SynchronizationContext MainThreadContext;

    // Start is called before the first frame update
    void Start()
    {
        ApplicationDataPath = Application.dataPath;
        MainThreadContext = SynchronizationContext.Current;

        listener = new HttpListener();
        listener.Prefixes.Add(adr);

        Debug.Log("### View server started on " + adr);
        listener.Start();

        //受信処理スレッド
        thread = new Thread(new ThreadStart(ReceiveThread));
        thread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetResponse(string res)
    {
        responseBody = res;
    }

    private void OnDestroy()
    {
        listener.Close();
        thread.Join();
    }

    private void ReceiveThread()
    {
        try
        {
            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                //Console.WriteLine(request.Url.LocalPath);

                HttpListenerResponse response = context.Response;
                string res = "";

                try
                {
                    switch (request.Url.LocalPath)
                    {
                        case "/":
                            res = File.ReadAllText(ApplicationDataPath + "/StreamingAssets/index.htm", new UTF8Encoding(false));
                            break;
                        case "/get.dat":
                            MainThreadContext.Post((obj) => {

                            }, null);
                            res = responseBody;
                            break;
                        case "/set.dat":
                            MainThreadContext.Post((obj) => {
                                //Hello MainThread
                            }, null);
                            res = responseBody;
                            break;
                        case "/script.js":
                            res = File.ReadAllText(ApplicationDataPath + "/StreamingAssets/script.js", new UTF8Encoding(false));
                            break;
                        case "/worker.js":
                            res = File.ReadAllText(ApplicationDataPath + "/StreamingAssets/worker.js", new UTF8Encoding(false));
                            break;
                        case "/style.css":
                            res = File.ReadAllText(ApplicationDataPath + "/StreamingAssets/style.css", new UTF8Encoding(false));
                            break;
                        default:
                            res = "404 Not found";
                            response.StatusCode = 404;
                            break;
                    }
                }
                catch (Exception e)
                {
                    response.StatusCode = 500;
                    res = "Internal Server Error";
                    Debug.Log(e);
                }

                byte[] buf = new UTF8Encoding(false).GetBytes(res);
                response.OutputStream.Write(buf, 0, buf.Length);
                response.OutputStream.Close();

                Thread.Sleep(30);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
}


