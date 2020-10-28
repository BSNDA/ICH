using System;
using System.ComponentModel;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;

namespace Nep5Proxy
{
    public class Nep5Proxy : SmartContract
    {
        // TODO: fill in ccmc script hash
        private static readonly byte[] CCMCScriptHash = "".HexToBytes(); // little endian
        private static readonly byte[] OperatorKey = "ok".AsByteArray();
        private static readonly byte[] ProxyHashPrefix = new byte[] { 0x01, 0x01 };
        private static readonly byte[] AssetHashPrefix = new byte[] { 0x01, 0x02 };
        private static readonly byte[] PauseKey = "pk".AsByteArray();
        private static readonly byte[] FromAssetHashMapKey = "fahmk".AsByteArray(); // "FromAssetList";

        // Dynamic Call
        delegate object DynCall(string method, object[] args); // dynamic call
        // Events
        public static event Action<byte[]> InitEvent;
        public static event Action<byte[], byte[]> TransferOwnershipEvent;
        public static event Action<byte[], byte[], BigInteger, byte[], byte[], BigInteger> LockEvent;
        public static event Action<byte[], byte[], BigInteger> UnlockEvent;
        public static event Action<BigInteger, byte[]> BindProxyHashEvent;
        public static event Action<byte[], BigInteger, byte[]> BindAssetHashEvent;
        
        private static readonly BigInteger chainId = 4;
        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Application)
            {
                byte[] callingScriptHash = ExecutionEngine.CallingScriptHash;
                if (method == "name") return Name();

                if (method == "init") return Init((byte[])args[0]);
                if (method == "pause") return Pause();
                if (method == "unpause") return Unpause();
                if (method == "isPaused") return IsPaused();
                if (method == "transferOwnership") return TransferOwnership((byte[])args[0]);
                if (method == "getOperator") return GetOperator();
                if (method == "bindProxyHash") return BindProxyHash((BigInteger)args[0], (byte[])args[1]);
                if (method == "bindAssetHash") return BindAssetHash((byte[])args[0], (BigInteger)args[1], (byte[])args[2]);
                if (method == "getAssetBalance") return GetAssetBalance((byte[])args[0]);
                if (method == "getProxyHash") return GetProxyHash((BigInteger)args[0]);
                if (method == "getAssetHash") return GetAssetHash((byte[])args[0], (BigInteger)args[1]);
                if (method == "getFromAssetHashes") return GetFromAssetHashes();
                if (method == "lock") return Lock((byte[])args[0], (byte[])args[1], (BigInteger)args[2], (byte[])args[3], (BigInteger)args[4]);
                if (method == "unlock") return Unlock((byte[])args[0], (byte[])args[1], (BigInteger)args[2], callingScriptHash);
                if (method == "upgrade")
                {
                    assert(args.Length == 9, "upgrade: args.Length != 9.");
                    byte[] script = (byte[])args[0];
                    byte[] plist = (byte[])args[1];
                    byte rtype = (byte)args[2];
                    ContractPropertyState cps = (ContractPropertyState)args[3];
                    string name = (string)args[4];
                    string version = (string)args[5];
                    string author = (string)args[6];
                    string email = (string)args[7];
                    string description = (string)args[8];
                    return Upgrade(script, plist, rtype, cps, name, version, author, email, description);
                }
            }
            assert(false, "invalid method-".AsByteArray().Concat(method.AsByteArray()).AsString());
            // After throw exception is enabled, code will never run below.
            return false;
        }

        [DisplayName("name")]
        public static string Name() => "NeoProxy";
        [DisplayName("init")]
        public static bool Init(byte[] _operator)
        {
            assert(Storage.Get(OperatorKey).Length == 0, "init: operator exist: ".AsByteArray().Concat(Storage.Get(OperatorKey)).AsString());
            assert(Runtime.CheckWitness(_operator), "init: CheckWitness failed");
            Storage.Put(OperatorKey, _operator);
            InitEvent(_operator);
            return true;
        }

        [DisplayName("pause")]
        public static bool Pause()
        {
            assert(Runtime.CheckWitness(GetOperator()), "pause: CheckWitness failed!");
            Storage.Put(PauseKey, new byte[] { 0x01 });
            return true;
        }


        [DisplayName("unpause")]
        public static bool Unpause()
        {
            assert(Runtime.CheckWitness(GetOperator()), "pause: CheckWitness failed!");
            Storage.Delete(PauseKey);
            return true;
        }
        [DisplayName("isPaused")]
        public static bool IsPaused()
        {
            return Storage.Get(PauseKey).Equals(new byte[] { 0x01 });
        }
        [DisplayName("transferOwnership")]
        public static bool TransferOwnership(byte[] newOperator)
        {
            assert(newOperator.Length == 20, "transferOwnership: newOperator.Length != 20");
            byte[] operator_ = Storage.Get(OperatorKey);
            assert(Runtime.CheckWitness(operator_), "transferOwnership: CheckWitness failed!");
            Storage.Put(OperatorKey, newOperator);
            TransferOwnershipEvent(operator_, newOperator);
            return true;
        }

        [DisplayName("getOperator")]
        public static byte[] GetOperator()
        {
            return Storage.Get(OperatorKey);
        }

        // add target proxy contract hash according to chain id into contract storage
        [DisplayName("bindProxyHash")]
        public static bool BindProxyHash(BigInteger toChainId, byte[] toProxyHash)
        {
            assert(toChainId > 0 && toChainId != chainId, "bindProxyHash: toChainId is negative or equal to 4.");
            assert(toProxyHash.Length > 0, "bindProxyHash: toProxyHash.Length == 0!");
            byte[] operator_ = Storage.Get(OperatorKey);
            assert(Runtime.CheckWitness(operator_), "bindProxyHash: CheckWitness failed, ".AsByteArray().Concat(operator_).AsString());
            Storage.Put(ProxyHashPrefix.Concat(toChainId.ToByteArray()), toProxyHash);
            BindProxyHashEvent(toChainId, toProxyHash);
            return true;
        }

        // add target asset contract hash according to local asset hash & chain id into contract storage
        [DisplayName("bindAssetHash")]
        public static bool BindAssetHash(byte[] fromAssetHash, BigInteger toChainId, byte[] toAssetHash)
        {
            assert(fromAssetHash.Length == 20, "bindAssetHash: fromAssetHash length != 20.");
            assert(toChainId > 0 && toChainId != chainId, "bindAssetHash: toChainId cannot be negative or equal to 4.");
            assert(toAssetHash.Length == 20, "bindAssetHash: oldAssetHash length != 20.");

            byte[] operator_ = Storage.Get(OperatorKey);
            assert(Runtime.CheckWitness(operator_), "bindAssetHash: CheckWitness failed, ".AsByteArray().Concat(operator_).AsString());

            // Add fromAssetHash into storage so as to be able to be transferred into newly upgraded contract
            assert(addFromAssetHash(fromAssetHash), "bindAssetHash: addFromAssetHash failed!");
            Storage.Put(AssetHashPrefix.Concat(fromAssetHash).Concat(toChainId.ToByteArray()), toAssetHash);
            BindAssetHashEvent(fromAssetHash, toChainId, toAssetHash);
            return true;
        }

        private static bool addFromAssetHash(byte[] newAssetHash)
        {
            Map<byte[], bool> assetHashMap = new Map<byte[], bool>();
            byte[] assetHashMapInfo = Storage.Get(FromAssetHashMapKey);
            if (assetHashMapInfo.Length == 0)
            {
                assetHashMap[newAssetHash] = true;
            }
            else
            {
                assetHashMap = (Map<byte[], bool>)assetHashMapInfo.Deserialize();
                if (!assetHashMap.HasKey(newAssetHash))
                {
                    assetHashMap[newAssetHash] = true;
                }
                else
                {
                    return true;
                }
            }
            // Make sure fromAssetHash has balanceOf method
            BigInteger balance = GetAssetBalance(newAssetHash);
            Storage.Put(FromAssetHashMapKey, assetHashMap.Serialize());
            return true;
        }

        [DisplayName("getAssetBalance")]
        public static BigInteger GetAssetBalance(byte[] assetHash)
        {
            byte[] curHash = ExecutionEngine.ExecutingScriptHash;
            BigInteger balance = (BigInteger)((DynCall)assetHash.ToDelegate())("balanceOf", new object[] { curHash });
            return balance;
        }

        // get target proxy contract hash according to chain id
        [DisplayName("getProxyHash")]
        public static byte[] GetProxyHash(BigInteger toChainId)
        {
            return Storage.Get(ProxyHashPrefix.Concat(toChainId.ToByteArray()));
        }

        // get target asset contract hash according to local asset hash & chain id
        [DisplayName("getAssetHash")]
        public static byte[] GetAssetHash(byte[] fromAssetHash, BigInteger toChainId)
        {
            return Storage.Get(AssetHashPrefix.Concat(fromAssetHash).Concat(toChainId.ToByteArray()));
        }

        [DisplayName("getFromAssetHashes")]
        public static byte[][] GetFromAssetHashes()
        {
            byte[] assetHashMapInfo = Storage.Get(FromAssetHashMapKey);
            if (assetHashMapInfo.Length == 0)
            {
                return new byte[][] { };
            }
            Map<byte[], bool> assetHashMap = (Map<byte[], bool>)assetHashMapInfo.Deserialize();
            return assetHashMap.Keys;
        }

        // used to lock asset into proxy contract
        [DisplayName("lock")]
        public static bool Lock(byte[] fromAssetHash, byte[] fromAddress, BigInteger toChainId, byte[] toAddress, BigInteger amount)
        {
            assert(fromAddress != ExecutionEngine.ExecutingScriptHash, "lock: fromAddress can't be proxy address.");
            assert(fromAssetHash.Length == 20, "lock: fromAssetHash SHOULD be 20-byte long.");
            assert(fromAddress.Length == 20, "lock: fromAddress SHOULD be 20-byte long.");
            assert(toAddress.Length > 0, "lock: toAddress SHOULD not be empty.");
            assert(amount > 0, "lock: amount SHOULD be greater than 0.");

            assert(!IsPaused(), "lock: proxy is locked");

            // get the proxy contract on target chain
            var toProxyHash = GetProxyHash(toChainId);
            assert(toProxyHash.Length > 0, "lock: toProxyHash SHOULD not be empty.");
            // get the corresbonding asset on to chain
            var toAssetHash = GetAssetHash(fromAssetHash, toChainId);
            assert(toAssetHash.Length > 0, "lock: toAssetHash SHOULD not be empty.");
            var Params = new object[] { fromAddress, ExecutionEngine.ExecutingScriptHash, amount };
            // transfer asset from fromAddress to proxy contract address, use dynamic call to call nep5 token's contract "transfer"
            bool success = (bool)((DynCall)fromAssetHash.ToDelegate())("transfer", Params);
            assert(success, "lock: Failed to transfer NEP5 token to Nep5Proxy.");

            // construct args for proxy contract on target chain
            var inputArgs = SerializeArgs(toAssetHash, toAddress, amount);
            // dynamic call CCMC
            success = (bool)((DynCall)CCMCScriptHash.ToDelegate())("CrossChain", new object[] { toChainId, toProxyHash, "unlock", inputArgs });
            assert(success, "lock: Failed to call CCMC.");
            LockEvent(fromAssetHash, fromAddress, toChainId, toAssetHash, toAddress, amount);

            return true;
        }

        // Methods of actual execution, used to unlock asset from proxy contract
        [DisplayName("unlock")]
        public static bool Unlock(byte[] inputBytes, byte[] fromProxyContract, BigInteger fromChainId, byte[] callingScriptHash)
        {
            //only allowed to be called by CCMC
            assert(callingScriptHash.Equals(CCMCScriptHash), "unlock: Only allowed to be called by CCMC.");

            byte[] proxyHash = Storage.Get(ProxyHashPrefix.Concat(fromChainId.ToByteArray()));

            // check the fromContract is stored, so we can trust it
            //assert(proxyHash.Equals(fromProxyContract), "unlock: fromProxyContract Not equal stored proxy hash.");
            if (fromProxyContract.AsBigInteger() != proxyHash.AsBigInteger())
            {
                Runtime.Notify("From proxy contract not found.");
                Runtime.Notify(fromProxyContract);
                Runtime.Notify(fromChainId);
                Runtime.Notify(proxyHash);
                return false;
            }
            assert(!IsPaused(), "lock: proxy is locked");

            // parse the args bytes constructed in source chain proxy contract, passed by multi-chain
            object[] results = DeserializeArgs(inputBytes);
            var toAssetHash = (byte[])results[0];
            var toAddress = (byte[])results[1];
            var amount = (BigInteger)results[2];
            assert(toAssetHash.Length == 20, "unlock: ToChain Asset script hash SHOULD be 20-byte long.");
            assert(toAddress.Length == 20, "unlock: ToChain Account address SHOULD be 20-byte long.");
            assert(amount >= 0, "ToChain Amount SHOULD not be less than 0.");

            /*var Params = new object[] { ExecutionEngine.ExecutingScriptHash, toAddress, amount };
            var contracthash = ExecutionEngine.ExecutingScriptHash;
            Runtime.Notify(contracthash);
            // transfer asset from proxy contract to toAddress
            bool success = (bool)((DynCall)toAssetHash.ToDelegate())("transfer", Params);*/
            byte[] currentHash = ExecutionEngine.ExecutingScriptHash; // this proxy contract hash
            var nep5Contract = (DynCall)toAssetHash.ToDelegate();
            bool success = (bool)nep5Contract("transfer", new object[] { currentHash, toAddress, amount });
            if (!success)
            {
                Runtime.Notify("Failed to transfer NEP5 token to toAddress.");
                return false;
            }
            /*assert(success, "unlock: Failed to transfer NEP5 token From Nep5Proxy to toAddress.");*/
            UnlockEvent(toAssetHash, toAddress, amount);
            return true;
        }

        // used to upgrade this proxy contract
        [DisplayName("upgrade")]
        public static bool Upgrade(byte[] newScript, byte[] paramList, byte returnType, ContractPropertyState cps, string name, string version, string author, string email, string description)
        {
            assert(Runtime.CheckWitness(Storage.Get(OperatorKey)), "upgrade: CheckWitness failed!");
            byte[] newContractHash = Hash160(newScript);
            assert(transferAssetsToNewContract(newContractHash), "upgrade: transfer asset into new contract hash failed!");
            Contract newContract = Contract.Migrate(newScript, paramList, returnType, cps, name, version, author, email, description);
            Runtime.Notify(new object[] { "upgrade", ExecutionEngine.ExecutingScriptHash, newContractHash });
            return true;
        }

        private static bool transferAssetsToNewContract(byte[] newContractHash)
        {
            // Try to transfer nep5 asset from old contract into new contract
            byte[] assetHashMapInfo = Storage.Get(FromAssetHashMapKey);
            if (assetHashMapInfo.Length > 0)
            {
                Map<byte[], bool> assetHashMap = (Map<byte[], bool>)assetHashMapInfo.Deserialize();
                byte[][] assetHashes = assetHashMap.Keys;

                byte[] self = ExecutionEngine.ExecutingScriptHash;
                BigInteger assetBalance;
                bool success;
                foreach (var assetHash in assetHashes)
                {
                    if (assetHashMap[assetHash] && assetHash.Length == 20)
                    {
                        assetBalance = (BigInteger)((DynCall)assetHash.ToDelegate())("balanceOf", new object[] { self });
                        if (assetBalance > 0)
                        {
                            success = (bool)((DynCall)assetHash.ToDelegate())("transfer", new Object[] { self, newContractHash, assetBalance });
                            assert(success, "upgrade: transfer Nep5 asset from old contract into new contract failed!");
                        }
                    }
                }
            }
            return true;
        }

        private static object[] DeserializeArgs(byte[] buffer)
        {
            var offset = 0;
            var res = ReadVarBytes(buffer, offset);
            var assetAddress = res[0];

            res = ReadVarBytes(buffer, (int)res[1]);
            var toAddress = res[0];

            res = ReadUint255(buffer, (int)res[1]);
            var amount = res[0];

            return new object[] { assetAddress, toAddress, amount };
        }

        private static object[] ReadUint255(byte[] buffer, int offset)
        {
            if (offset + 32 > buffer.Length)
            {
                Runtime.Notify("Length is not long enough");
                return new object[] { 0, -1 };
            }
            return new object[] { buffer.Range(offset, 32).ToBigInteger(), offset + 32 };
        }

        // return [BigInteger: value, int: offset]
        private static object[] ReadVarInt(byte[] buffer, int offset)
        {
            var res = ReadBytes(buffer, offset, 1); // read the first byte
            var fb = (byte[])res[0];
            if (fb.Length != 1)
            {
                Runtime.Notify("Wrong length");
                return new object[] { 0, -1 };
            }
            var newOffset = (int)res[1];
            if (fb == new byte[] { 0xFD })
            {
                return new object[] { buffer.Range(newOffset, 2).ToBigInteger(), newOffset + 2 };
            }
            else if (fb == new byte[] { 0xFE })
            {
                return new object[] { buffer.Range(newOffset, 4).ToBigInteger(), newOffset + 4 };
            }
            else if (fb == new byte[] { 0xFF })
            {
                return new object[] { buffer.Range(newOffset, 8).ToBigInteger(), newOffset + 8 };
            }
            else
            {
                return new object[] { fb.ToBigInteger(), newOffset };
            }
        }

        // return [byte[], new offset]
        private static object[] ReadVarBytes(byte[] buffer, int offset)
        {
            var res = ReadVarInt(buffer, offset);
            var count = (int)res[0];
            var newOffset = (int)res[1];
            return ReadBytes(buffer, newOffset, count);
        }

        // return [byte[], new offset]
        private static object[] ReadBytes(byte[] buffer, int offset, int count)
        {
            assert(offset + count <= buffer.Length, "ReadBytes, offset + count exceeds buffer.Length.");
            return new object[] { buffer.Range(offset, count), offset + count };
        }

        private static byte[] SerializeArgs(byte[] assetHash, byte[] address, BigInteger amount)
        {
            var buffer = new byte[] { };
            buffer = WriteVarBytes(assetHash, buffer);
            buffer = WriteVarBytes(address, buffer);
            buffer = WriteUint255(amount, buffer);
            return buffer;
        }

        private static byte[] WriteUint255(BigInteger value, byte[] source)
        {
            assert(value >= 0, "Value out of range of uint255.");
            var v = PadRight(value.ToByteArray(), 32);
            return source.Concat(v); // no need to concat length, fix 32 bytes
        }

        private static byte[] WriteVarInt(BigInteger value, byte[] Source)
        {
            if (value < 0)
            {
                return Source;
            }
            else if (value < 0xFD)
            {
                return Source.Concat(value.ToByteArray());
            }
            else if (value <= 0xFFFF) // 0xff, need to pad 1 0x00
            {
                byte[] length = new byte[] { 0xFD };
                var v = PadRight(value.ToByteArray(), 2);
                return Source.Concat(length).Concat(v);
            }
            else if (value <= 0XFFFFFFFF) //0xffffff, need to pad 1 0x00 
            {
                byte[] length = new byte[] { 0xFE };
                var v = PadRight(value.ToByteArray(), 4);
                return Source.Concat(length).Concat(v);
            }
            else //0x ff ff ff ff ff, need to pad 3 0x00
            {
                byte[] length = new byte[] { 0xFF };
                var v = PadRight(value.ToByteArray(), 8);
                return Source.Concat(length).Concat(v);
            }
        }

        private static byte[] WriteVarBytes(byte[] value, byte[] Source)
        {
            return WriteVarInt(value.Length, Source).Concat(value);
        }

        // add padding zeros on the right
        private static byte[] PadRight(byte[] value, int length)
        {
            var l = value.Length;
            if (l > length)
                return value.Range(0, length);
            for (int i = 0; i < length - l; i++)
            {
                value = value.Concat(new byte[] { 0x00 });
            }
            return value;
        }

        private static void assert(bool condition, string msg)
        {
            if (!condition)
            {
                // TODO: uncomment next line on mainnet
                //throw new InvalidOperationException("Unequal result!");
                Runtime.Notify("Nep5Proxy ".AsByteArray().Concat(msg.AsByteArray()).AsString());
            }
        }

    }
}