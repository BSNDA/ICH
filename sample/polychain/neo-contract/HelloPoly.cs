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
		// NEO在Poly网络中所对应的链ID
        private static readonly BigInteger neoChainID = 4;

        // 应用合约哈希默认值
        private static readonly byte[] ProxyHashPrefix = new byte[] { 0x01, 0x01 };

        // 动态回调
        delegate object DynCall(string method, object[] args);

        // 设置管理合约通知事件
        public static event Action<byte[]> SetManagerProxyEvent;

        // 设置应用合约通知事件
        public static event Action<BigInteger, byte[]> BindProxyHashEvent;

        // 设置Say通知事件
        public static event Action<BigInteger, byte[]> SayEvent;

        // 设置Hear通知事件
        public static event Action<BigInteger, byte[], byte[]> HearEvent;


        /// <summary>
        /// 函数主入口
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
                if (method == "say") return Say((BigInteger)args[0], (byte[])args[1], (byte[])args[2]);
                if (method == "hear") return Hear((byte[])args[0], (byte[])args[1], (BigInteger)args[2], callingScriptHash);
            }
            HelloPoly.Notify(false, "[HelloPoly]-invalid method-".AsByteArray().Concat(method.AsByteArray()).AsString());
            return false;
        }

        /// <summary>
        /// 设置管理合约地址
        /// </summary>
        /// <param name="ccmcProxyHash">部署在NEO网络上所对应的管理合约地址所对应的小端序（如管理合约地址0x69d0ba0866ee3d9abd19b06ad8ac6f49023e19b8，则小端序为b8193e02496facd86ab019bd9a3dee6608bad069）</param>
        /// <returns></returns>
        [DisplayName("setManagerProxy")]
        public static bool SetManagerProxy(byte[] ccmcProxyHash)
        {
            Storage.Put(ProxyHashPrefix.Concat(neoChainID.ToByteArray()), ccmcProxyHash);
			
            HelloPoly.SetManagerProxyEvent(ccmcProxyHash);
			
            return true;
        }

        /// <summary>
        /// 绑定部署在目标链上所对应的应用合约地址
        /// </summary>
        /// <param name="toChainId">目标链在Poly网络中所对应的链ID</param>
        /// <param name="toProxyHash">部署在目标链上所对应的应用合约地址</param>
        /// <returns></returns>
        [DisplayName("bindProxyHash")]
        public static bool BindProxyHash(BigInteger toChainId, byte[] toProxyHash)
        {
            Storage.Put(ProxyHashPrefix.Concat(toChainId.ToByteArray()), toProxyHash);
			
            HelloPoly.BindProxyHashEvent(toChainId, toProxyHash);
			
            return true;
        }


        /// <summary>
        /// 此方法用于对其它目标链进行跨链调用（此方法可自行定义）
        /// </summary>
        /// <param name="toChainId">目标链在Poly网络中所对应的链ID</param>
        /// <param name="msg">目标链应用合约所需要传递的跨链信息</param>
        /// <returns></returns>
       [DisplayName("say")]
        public static bool Say(BigInteger toChainId, byte[] msg)
        {
            // 获取目标链上的应用合约
            var toProxyHash = HelloPoly.GetProxyHash(toChainId);
			
            // 获取CCMC合约地址
            var ccmcScriptHash = HelloPoly.GetProxyHash(neoChainID);
			
            // 跨链调用
            bool success = (bool)((DynCall)ccmcScriptHash.ToDelegate())("CrossChain", new object[] { toChainId, toProxyHash, "hear", msg });
            
			HelloPoly.Notify(success, "[HelloPoly]-Say: Failed to call CCMC.");
			
            // 事件通知
            HelloPoly.SayEvent(toChainId, toProxyHash);
            return true;
        }


        /// <summary>
        /// 此方法用于对其它目标链进行跨链调用（此方法可自行定义）
        /// </summary>
        /// <param name="fromChainId">源链在Poly网络中所对应的链ID</param>
        /// <param name="toChainId">目标链在Poly网络中所对应的链ID</param>
        /// <param name="msg">接收到源链发送的跨链信息</param>
        /// <param name="callingScriptHash">回调脚本哈希</param>
        /// <returns></returns>
        [DisplayName("hear")]
        public static bool Hear(byte[] inputBytes, byte[] fromProxyContract, BigInteger fromChainId, byte[] callingScriptHash)
        {
            // 写入账本
            Storage.Put(fromProxyContract, inputBytes);
			
            // 事件通知
            HearEvent(fromChainId, fromProxyContract, inputBytes);
			
            return true;
        }


        /// <summary>
        /// 获取已经绑定的目标链所对应的应用合约地址
        /// </summary>
        /// <param name="toChainId">目标链在Poly网络中所对应的链ID</param>
        /// <returns></returns>
        [DisplayName("getProxyHash")]
        public static byte[] GetProxyHash(BigInteger toChainId)
        {
            return Storage.Get(ProxyHashPrefix.Concat(toChainId.ToByteArray()));
        }

        /// <summary>
        /// 信息通知
        /// </summary>
        /// <param name="condition">是否信息通知</param>
        /// <param name="msg">通知消息</param>
        private static void Notify(bool condition, string msg)
        {
            if (!condition)
            {
                Runtime.Notify("HelloPoly ".AsByteArray().Concat(msg.AsByteArray()).AsString());
            }
        }
    }
}