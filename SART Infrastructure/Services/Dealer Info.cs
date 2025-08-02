using Segway.Service.Objects;
using Segway.Syteline.Client.REST;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Segway.Modules.SART_Infrastructure
{
    public class Dealer_Info : Dealer_Info_Interface
    {
        public static readonly String Name = "Dealer Information";

        #region Dealers

        private Dictionary<String, String> _Dealers;

        private static Boolean Loading = false;

        /// <summary>Property Dealer_List of type Dictionary<int, String></summary>
        public Dictionary<String, String> Dealers
        {
            get
            {
                if (_Dealers == null) LoadDealer();
                return _Dealers;
            }
            set { _Dealers = value; }
        }

        #endregion

        #region Accounts

        private Dictionary<String, String> _Accounts;

        /// <summary>Property Accounts of type Dictionary<String,int></summary>
        public Dictionary<String, String> Accounts
        {
            get
            {
                if (_Accounts == null) LoadDealer();

                return _Accounts;
            }
            set
            {
                _Accounts = value;
            }
        }

        #endregion

        #region Dealer_List

        private List<String> _Dealer_List;

        /// <summary>Property Dealer_List of type List<String></summary>
        public List<String> Dealer_List
        {
            get
            {
                if (_Dealer_List == null)
                {
                    _Dealer_List = new List<string>();
                }
                if (_Dealer_List.Count == 0)
                {
                    _Dealer_List = new List<string>(Dealers.Values);
                    _Dealer_List.Sort();
                    _Dealer_List.Insert(0, "");
                }
                return _Dealer_List;
            }
            set { _Dealer_List = value; }
        }

        #endregion

        public void LoadDealer()
        {
            _Dealers = new Dictionary<String, String>();
            _Accounts = new Dictionary<String, String>();

            if (InfrastructureModule.Token != null)
            {
                if (Loading == true)
                {
                    DateTime timeout = DateTime.Now.AddSeconds(30);
                    while (DateTime.Now < timeout)
                    {
                        if (Loading == false) break;
                        Thread.Sleep(250);
                    }
                }

                try
                {
                    Loading = true;
                    List<(String custNum, String contact)> dealers = Syteline_Customer_Web_Service_Client_REST.Select_REGIONAL_DEALERS(InfrastructureModule.Token, InfrastructureModule.RegionSettings);
                    if (dealers != null)
                    {
                        foreach (var dealer in dealers)
                        {
                            _Dealers[dealer.custNum] = dealer.contact;
                            if (String.IsNullOrEmpty(dealer.contact) == false) _Accounts[dealer.contact] = dealer.custNum;
                        }

                        Dealer_List = new List<String>(Accounts.Keys);
                        Dealer_List.Add("");
                        Dealer_List.Sort();
                    }
                }
                finally
                {
                    Loading = false;
                }
            }
        }
    }
}
