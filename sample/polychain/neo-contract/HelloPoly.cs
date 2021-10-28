using System;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;

namespace HelloPoly
{
    public class HelloPoly : SmartContract
    {
	// The chain ID corresponding to the NEO framework in the Poly network
        private static readonly BigInteger neoChainID = 4;

        // Default hash value of the application contract 
        private static readonly byte[] ProxyHashPrefix = new byte[] { 0x01, 0x01 };

        //  Dynamic callbacks
        delegate object DynCall(string method, object[] args);

        // Set management contract notification event
        public static event Action<byte[]> SetManagerProxyEvent;

        // Set application contract notification event
        public static event Action<BigInteger, byte[]> BindProxyHashEvent;

        // Set Say menthod notification event
        public static event Action<BigInteger, byte[]> SayEvent;

        // Set Hear method notification event
        public static event Action<BigInteger, byte[], byte[]> HearEvent;


        /// <summary>
        /// Main function entrance
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Application)
            {
                byte[] callingScriptHash = ExecutionEngine.CallingScriptHash;
                if (method == "setManagerProxy") return SetManagerProxy((byte[])args[0]);
                if (method == "bindProxyHash") return BindProxyHash((BigInteger)args[0], (byte[])args[1]);
                if (method == "say") return Say((BigInteger)args[0], (string)args[1], (byte[])args[2]);
                if (method == "hear") return Hear((byte[])args[0], (byte[])args[1], (BigInteger)args[2], callingScriptHash);
            }
            HelloPoly.Notify(false, "[HelloPoly]-invalid method-".AsByteArray().Concat(method.AsByteArray()).AsString());
            return false;
        }


        /// <summary>
        /// Set up the management contract address
        /// </summary>
        /// <param name="ccmcProxyHash">The little-endian corresponding to the management contract address deployed on the NEO network (e.g., if the management contract address is 0x69d0ba0866ee3d9abd19b06ad8ac6f49023e19b8, then the little-endian is b8193e02496facd86ab019bd9a3dee6608bad069)</param>
        /// <returns></returns>
        [DisplayName("setManagerProxy")]
        public static bool SetManagerProxy(byte[] ccmcProxyHash)
        {
            Storage.Put(ProxyHashPrefix.Concat(neoChainID.ToByteArray()), ccmcProxyHash);
			
            HelloPoly.SetManagerProxyEvent(ccmcProxyHash);
			
            return true;
        }


        /// <summary>
        /// Bind the address of the application contract deployed on the target chain
        /// </summary>
        /// <param name="toChainId">The chain ID corresponding to the target chain in the Poly network</param>
        /// <param name="toProxyHash">The address of the application contract deployed on the target chain</param>
        /// <returns></returns>
        [DisplayName("bindProxyHash")]
        public static bool BindProxyHash(BigInteger toChainId, byte[] toProxyHash)
        {
            Storage.Put(ProxyHashPrefix.Concat(toChainId.ToByteArray()), toProxyHash);
			
            HelloPoly.BindProxyHashEvent(toChainId, toProxyHash);
			
            return true;
        }


        /// <summary>
        /// This method is used to make cross-chain calls to other target chains (this method can be defined by users)
        /// </summary>
        /// <param name="toChainId">The chain ID corresponding to the target chain in the Poly network</param>
        /// <param name="method">Application contract method name of the target chain</param>
        /// <param name="msg">Cross-chain information that needs to be passed by the target chain application contract</param>
        /// <returns></returns>
       [DisplayName("say")]
        public static bool Say(BigInteger toChainId,string method, byte[] msg)
        {
            // Get the application contract on the target chain
            var toProxyHash = HelloPoly.GetProxyHash(toChainId);
			
            // Get CCMC contract address
            var ccmcScriptHash = HelloPoly.GetProxyHash(neoChainID);
			
            // Cross-chain calls
            bool success = (bool)((DynCall)ccmcScriptHash.ToDelegate())("CrossChain", new object[] { toChainId, toProxyHash, method, msg });
            
	    HelloPoly.Notify(success, "[HelloPoly]-Say: Failed to call CCMC.");
			
            // Event Notification
            HelloPoly.SayEvent(toChainId, toProxyHash);
            return true;
        }


        /// <summary>
        /// This method is used to make cross-chain calls to other target chains (this method can be defined by users)
        /// </summary>
        /// <param name="fromChainId">The chain ID corresponding to the source chain in the Poly network</param>
        /// <param name="toChainId">The chain ID corresponding to the target chain in the Poly network</param>
        /// <param name="msg">Receive cross-chain messages from the source chain</param>
        /// <param name="callingScriptHash">Callback script hash</param>
        /// <returns></returns>
        [DisplayName("hear")]
        public static bool Hear(byte[] inputBytes, byte[] fromProxyContract, BigInteger fromChainId, byte[] callingScriptHash)
        {
            // input the ledger information
            Storage.Put(fromProxyContract, inputBytes);
			
            // Event Notification
            HearEvent(fromChainId, fromProxyContract, inputBytes);
			
            return true;
        }


        /// <summary>
        /// Get the address of the application contract corresponding to the bound target chain
        /// </summary>
        /// <param name="toChainId">The chain ID corresponding to the target chain in the Poly network</param>
        /// <returns></returns>
        [DisplayName("getProxyHash")]
        public static byte[] GetProxyHash(BigInteger toChainId)
        {
            return Storage.Get(ProxyHashPrefix.Concat(toChainId.ToByteArray()));
        }


        /// <summary>
        /// Information Notification
        /// </summary>
        /// <param name="condition">Whether send information notification</param>
        /// <param name="msg">Notification Message</param>
        private static void Notify(bool condition, string msg)
        {
            if (!condition)
            {
                Runtime.Notify("HelloPoly ".AsByteArray().Concat(msg.AsByteArray()).AsString());
            }
        }
    }
}
