/*
 * daap-sharp
 * Copyright (C) 2005  James Willcox <snorp@snorp.net>
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

using Mono.Zeroconf;

namespace DAAP {

    public delegate void ServiceHandler (object o, ServiceArgs args);

    public class ServiceArgs : EventArgs {

        private Service service;
        
        public Service Service {
            get { return service; }
        }
        
        public ServiceArgs (Service service) {
            this.service = service;
        }
    }

    public class Service {
        private IPAddress address;
        private ushort port;
        private string name;
        private bool isprotected;

        public IPAddress Address {
            get { return address; }
        }

        public ushort Port {
            get { return port; }
        }

        public string Name {
            get { return name; }
        }

        public bool IsProtected {
            get { return isprotected; }
        }

        public Service (IPAddress address, ushort port, string name, bool isprotected) {
            this.address = address;
            this.port = port;
            this.name = name;
            this.isprotected = isprotected;
        }

        public override string ToString()
        {
            return String.Format("{0}:{1} ({2})", Address, Port, Name);
        }
    }
    
    
    public class ServiceLocator {
        
        private ServiceBrowser browser;
        private Hashtable services = new Hashtable ();
        private bool showLocals = false;
        
        public event ServiceHandler Found;
        public event ServiceHandler Removed;
        
        public bool ShowLocalServices {
            get { return showLocals; }
            set { showLocals = value; }
        }
        
        public IEnumerable Services {
            get { return services; }
        }
        
        public void Start () {
            if (browser != null) {
                Stop ();
            }
        
            browser = new ServiceBrowser ();
            browser.ServiceAdded += OnServiceAdded;
            browser.ServiceRemoved += OnServiceRemoved;
            browser.Browse ("_daap._tcp", null);
        }
        
        public void Stop () {
            browser.Dispose ();
            browser = null;
            services.Clear ();
        }
        
        private void OnServiceAdded (object o, ServiceBrowseEventArgs args) {
            args.Service.Resolved += OnServiceResolved;
            args.Service.Resolve ();
        }
        
        private void OnServiceResolved (object o, ServiceResolvedEventArgs args) {
            string name = args.Service.Name;

            if (services[name] != null) {
                return; // we already have it somehow
            }
            
            bool pwRequired = false;

            // iTunes tacks this on to indicate a passsword protected share.  Ugh.
            if (name.EndsWith ("_PW")) {
                name = name.Substring (0, name.Length - 3);
                pwRequired = true;
            }
            
            IResolvableService service = (IResolvableService) args.Service;
            
            foreach(TxtRecordItem item in service.TxtRecord) {
                if(item.Key.ToLower () == "password") {
                    pwRequired = item.ValueString.ToLower () == "true";
                } else if (item.Key.ToLower () == "machine name") {
                    name = item.ValueString;
                }
            }
            
            IPAddress address = args.Service.HostEntry.AddressList[0];
            if (address.AddressFamily == AddressFamily.InterNetworkV6) {
                // XXX: Workaround a Mono bug where we can't resolve IPv6 addresses properly, so we fix it here
                address = Dns.GetHostEntry (args.Service.HostEntry.HostName).AddressList[0];
            }
            
            DAAP.Service svc = new DAAP.Service (address, (ushort)service.Port, 
                name, pwRequired);
            
            services[svc.Name] = svc;
            
            if (Found != null)
                Found (this, new ServiceArgs (svc)); 
        }
        
        private void OnServiceRemoved (object o, ServiceBrowseEventArgs args) {
            Service svc = (Service) services[args.Service.Name];
            if (svc != null) {
                services.Remove (svc.Name);

                if (Removed != null)
                    Removed (this, new ServiceArgs (svc));
            }
        }
    }
}
