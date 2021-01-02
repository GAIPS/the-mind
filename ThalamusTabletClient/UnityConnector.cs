using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using CookComputing.XmlRpc;


public class UnityListener : XmlRpcListenerService, IUnityTabletSubscriber
{
    private ThalamusConnector _thalamusCS;

    public UnityListener(ThalamusConnector thalamusCS)
    {
        _thalamusCS = thalamusCS;
    }

    public void Dispose() { }

    public void ConnectToGM(int id, string name)
    {
        _thalamusCS.TypifiedPublisher.ConnectToGM(id, name);
    }

    public void SendBudgetAllocation(int tabletID, int envAllocation)
    {
        _thalamusCS.TypifiedPublisher.SendBudgetAllocation(tabletID, envAllocation);
    }

    public void Disconnect(int id)
    {
        _thalamusCS.TypifiedPublisher.Disconnect(id);
    }
}

public class UnityConnector
{
    private System.Random rand = new System.Random();
    private HttpListener _listener;
    private bool _serviceRunning;
    private int _localPort;
    private bool _shutdown;
    List<HttpListenerContext> _httpRequestsQueue = new List<HttpListenerContext>();
    private Thread _dispatcherThread;
    private Thread _messageDispatcherThread;

    private string _remoteUri = "";
    public IUnityTabletPublisher RPCProxy { private set; get; }
    private int _remotePort;
    private string _remoteAddress = "localhost";
    private ThalamusConnector _thalamusClient;

    public UnityConnector(ThalamusConnector thalamusClient, string address, int port)
    {
        _thalamusClient = thalamusClient;

        _remoteAddress = address;
        _localPort = port;
        _remotePort = port + 1;
        _remoteUri = String.Format("http://{0}:{1}/", _remoteAddress, _remotePort);
            
        RPCProxy = XmlRpcProxyGen.Create<IUnityTabletPublisher>();
        /*rpcProxy.Expect100Continue = true;
        rpcProxy.KeepAlive = false;*/
        RPCProxy.Timeout = 5000;
        RPCProxy.Url = _remoteUri;

        _dispatcherThread = new Thread(new ThreadStart(DispatcherThread));
        _messageDispatcherThread = new Thread(new ThreadStart(MessageDispatcher));
        _dispatcherThread.Start();
        _messageDispatcherThread.Start();

    }

    #region rpc stuff

    public void Dispose()
    {
        _shutdown = true;

        try
        {
            if (_listener != null) _listener.Stop();
        }
        catch { }

        try
        {
            if (_dispatcherThread != null) _dispatcherThread.Join();
        }
        catch { }

        try
        {
            if (_messageDispatcherThread != null) _messageDispatcherThread.Join();
        }
        catch { }

    }

    public void DispatcherThread()
    {
        while (!_serviceRunning)
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(string.Format("http://*:{0}/", _localPort));
                _listener.Start();
                _thalamusClient.Debug("XMLRPC Listening on " + string.Format("http://*:{0}/", _localPort));
                _serviceRunning = true;
            }
            catch
            {
                _localPort++;
                _serviceRunning = false;
            }
        }

        while (!_shutdown)
        {
            try
            {
                HttpListenerContext context = _listener.GetContext();
                lock (_httpRequestsQueue)
                {
                    _httpRequestsQueue.Add(context);
                }
            }
            catch (Exception)
            {
                    
                _serviceRunning = false;
                if (_listener != null)
                    _listener.Close();
            }
        }
    }

    public void MessageDispatcher()
    {
        while (!_shutdown)
        {
            bool performSleep = true;
            try
            {
                if (_httpRequestsQueue.Count > 0)
                {
                    performSleep = false;
                    List<HttpListenerContext> httpRequests;
                    lock (_httpRequestsQueue)
                    {
                        httpRequests = new List<HttpListenerContext>(_httpRequestsQueue);
                        _httpRequestsQueue.Clear();
                    }
                    foreach (HttpListenerContext r in httpRequests)
                    {
                        //ProcessRequest(r);
                        (new Thread(new ParameterizedThreadStart(ProcessRequest))).Start(r);
                        performSleep = false;
                    }
                }


            }
            catch (Exception)
            {
            }
            if (performSleep) Thread.Sleep(10);
        }
    }

    public void ProcessRequest(object oContext)
    {
        try
        {
            XmlRpcListenerService svc = new UnityListener(_thalamusClient);
            svc.ProcessRequest((HttpListenerContext)oContext);
        }
        catch (Exception)
        {
        }

    }

    #endregion
}
