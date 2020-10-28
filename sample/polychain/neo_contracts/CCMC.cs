using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace CrossChainContract
{
    public class NeoCrossChainManager : SmartContract
    {
        //Reuqest prefix
        private static readonly byte[] requestIDPrefix = new byte[] { 0x01, 0x01 };
        private static readonly byte[] requestPreifx = new byte[] { 0x01, 0x02 };

        //Header prefix
        private static readonly byte[] currentEpochHeightPrefix = new byte[] { 0x02, 0x01 };
        private static readonly byte[] mCKeeperPubKeysPrefix = new byte[] { 0x02, 0x04 };

        //tx prefix
        private static readonly byte[] transactionPrefix = new byte[] { 0x03, 0x01 };

        //constant
        private static readonly int MCCHAIN_PUBKEY_LEN = 67;
        private static readonly int MCCHAIN_SIGNATURE_LEN = 65;

        //动态调用
        delegate object DyncCall(string method, object[] args);

        //------------------------------event--------------------------------
        //CrossChainLockEvent "from address" "from contract" "to chain id" "key" "tx param"
        public static event Action<byte[], byte[], BigInteger, byte[], byte[]> CrossChainLockEvent;
        //CrossChainUnlockEvent fromChainID, TxParam.toContract, txHash
        public static event Action<BigInteger, byte[], byte[]> CrossChainUnlockEvent;
        //Sync Genesis Header Event Height, rawHeaders
        public static event Action<BigInteger, byte[]> InitGenesisBlockEvent;
        //更换联盟链公式
        public static event Action<BigInteger, byte[]> ChangeBookKeeperEvent;
        //同步区块头
        public static event Action<BigInteger, byte[]> SyncBlockHeaderEvent;
        public static object Main(string operation, object[] args)
        {
            byte[] caller = ExecutionEngine.CallingScriptHash;
            if (operation == "CrossChain")// 发起跨链交易
            {
                return CrossChain((BigInteger)args[0], (byte[])args[1], (byte[])args[2], (byte[])args[3], caller);
            }
            else if (operation == "ChangeBookKeeper") // 更新关键区块头公钥
            {
                return ChangeBookKeeper((byte[])args[0], (byte[])args[1], (byte[])args[2]);
            }
            else if (operation == "InitGenesisBlock")// 初始化创世块
            {
                return InitGenesisBlock((byte[])args[0], (byte[])args[1]);
            }
            else if (operation == "VerifyAndExecuteTx")// 执行跨链交易
            {
                return VerifyAndExecuteTx((byte[])args[0], (byte[])args[1], (byte[])args[2], (byte[])args[3], (byte[])args[4]);
            }
            else if (operation == "currentSyncHeight")
            {
                return Storage.Get(currentEpochHeightPrefix).AsBigInteger();
            }
            else if (operation == "VerifyAndExecuteTxTest")
            {
                return VerifyAndExecuteTxTest((byte[])args[0], (byte[])args[1]);
            }
            else if (operation == "verifySignList")
            {
                return verifySigTest((byte[])args[0], (byte[])args[1]);
            }
            else if (operation == "getbookkeepers")
            {
                return getBookKeepers();
            }
            return false;
        }

        public static bool VerifyAndExecuteTx(byte[] proof, byte[] RawHeader, byte[] headerProof, byte[] currentRawHeader, byte[] signList)
        {
            Header txheader = deserializHeader(RawHeader);
            byte[][] keepers = (byte[][])Storage.Get(mCKeeperPubKeysPrefix).Deserialize();
            int n = keepers.Length;
            int m = n - (n - 1) / 3;
            BigInteger currentEpochHeight = Storage.Get(currentEpochHeightPrefix).Concat(new byte[] { 0x00 }).AsBigInteger();
            byte[] StateRootValue = new byte[] { 0x00 };
            if (txheader.height >= currentEpochHeight)
            {
                if (!verifySig(RawHeader, signList, keepers, m))
                {
                    Runtime.Notify("Verify RawHeader signature failed!");
                    return false;
                }
            }
            else
            {
                if (!verifySig(currentRawHeader, signList, keepers, m))
                {
                    Runtime.Notify("Verify currentRawHeader signature failed!");
                    return false;
                }
                Header currentHeader = deserializHeader(currentRawHeader);
                StateRootValue = MerkleProve(headerProof, currentHeader.blockRoot);
                byte[] RawHeaderHash = Hash256(RawHeader);
                if (!StateRootValue.Equals(RawHeaderHash))
                {
                    Runtime.Notify("Verify block proof signature failed!");
                    return false;
                }
            }
            // Through rawHeader.CrossStateRoot, the toMerkleValue or cross chain msg can be verified and parsed from proof
            StateRootValue = MerkleProve(proof, txheader.crossStatesRoot);
            if (StateRootValue.Equals(new byte[] { 0x00 }))
            {
                Runtime.Notify("cross chain Proof verify error");
                return false;
            }
            ToMerkleValue merkleValue = deserializMerkleValue(StateRootValue);
            //check by txid
            if (Storage.Get(transactionPrefix.Concat(merkleValue.fromChainID.AsByteArray()).Concat(merkleValue.txHash)).AsBigInteger() == 1)
            {
                Runtime.Notify("Transaction has been executed");
                return false;
            }
            //check to chainID
            if (merkleValue.TxParam.toChainID != 4)
            {
                Runtime.Notify("Not Neo crosschain tx");
                return false;
            }
            //run croos chain tx
            if (ExecuteCrossChainTx(merkleValue))
            {
                Runtime.Notify("Tx execute success");
            }
            else
            {
                Runtime.Notify("Tx execute fail");
                return false;
            }

            //event
            CrossChainUnlockEvent(merkleValue.fromChainID, merkleValue.TxParam.toContract, merkleValue.txHash);
            return true;
        }

        public static object VerifyAndExecuteTxTest(byte[] proof, byte[] root)
        {
            byte[] CrossChainParams = MerkleProve(proof, root);
            if (CrossChainParams.Equals(new byte[] { 0x00 }))
            {
                Runtime.Notify("Proof verify error");
                return false;
            }
            ToMerkleValue merkleValue = deserializMerkleValue(CrossChainParams);
            if (Storage.Get(transactionPrefix.Concat(merkleValue.txHash)).AsBigInteger() == 1)
            {
                Runtime.Notify("Transaction has been executed");
                return false;
            }
            if (merkleValue.TxParam.toChainID != 4)
            {
                Runtime.Notify("Not Neo crosschain tx");
                return false;
            }
            Runtime.Notify("proof and root are correct");

            return true;
        }

        public static object getBookKeepers()
        {
            byte[][] keepers = (byte[][])Storage.Get(mCKeeperPubKeysPrefix).Deserialize();
            return keepers;
        }

        public static bool CrossChain(BigInteger toChainID, byte[] toChainAddress, byte[] functionName, byte[] args, byte[] caller)
        {
            var tx = (Transaction)ExecutionEngine.ScriptContainer;

            CrossChainTxParameter para = new CrossChainTxParameter
            {
                toChainID = toChainID,
                toContract = toChainAddress,
                method = functionName,
                args = args,

                txHash = tx.Hash,
                crossChainID = SmartContract.Sha256(ExecutionEngine.ExecutingScriptHash.Concat(tx.Hash)),
                fromContract = caller
            };
            var requestId = getRequestID(toChainID);
            var resquestKey = putRequest(toChainID, requestId, para);

            //event
            CrossChainLockEvent(caller, para.fromContract, toChainID, resquestKey, para.args);
            return true;
        }

        public static bool ChangeBookKeeper(byte[] rawHeader, byte[] pubKeyList, byte[] signList)
        {
            Header header = deserializHeader(rawHeader);
            if (header.height == 0)
            {
                return InitGenesisBlock(rawHeader, pubKeyList);
            }
            BigInteger latestHeight = Storage.Get(currentEpochHeightPrefix).Concat(new byte[] { 0x00 }).AsBigInteger();
            if (latestHeight > header.height)
            {
                Runtime.Notify("The height of header illegal");
                return false;
            }
            if (header.nextBookKeeper.Length != 20)
            {
                Runtime.Notify("The nextBookKeeper of header is illegal");
                return false;
            }
            byte[][] keepers = (byte[][])Storage.Get(mCKeeperPubKeysPrefix).Deserialize();
            int n = keepers.Length;
            int m = n - (n - 1) / 3;
            if (!verifySig(rawHeader, signList, keepers, m))
            {
                Runtime.Notify("Verify signature failed");
                return false;
            }
            BookKeeper bookKeeper = verifyPubkey(pubKeyList);
            if (header.nextBookKeeper != bookKeeper.nextBookKeeper)
            {
                Runtime.Notify("NextBookers illegal");
                return false;
            }
            Storage.Put(currentEpochHeightPrefix, header.height);
            Storage.Put(mCKeeperPubKeysPrefix, bookKeeper.keepers.Serialize());
            ChangeBookKeeperEvent(header.height, rawHeader);
            return true;
        }

        public static bool InitGenesisBlock(byte[] rawHeader, byte[] pubKeyList)
        {
            if (IsGenesised() != 0) return false;
            Header header = deserializHeader(rawHeader);
            Runtime.Notify("header deserialize");
            if (pubKeyList.Length % MCCHAIN_PUBKEY_LEN != 0)
            {
                Runtime.Notify("Length of pubKeyList is illegal");
                return false;
            }
            BookKeeper bookKeeper = verifyPubkey(pubKeyList);
            Runtime.Notify("header deserialize");
            if (header.nextBookKeeper != bookKeeper.nextBookKeeper)
            {
                Runtime.Notify("NextBookers illegal");
            }
            Storage.Put(currentEpochHeightPrefix, header.height);
            Storage.Put("IsInitGenesisBlock", 1);
            Map<BigInteger, BigInteger> MCKeeperHeight = new Map<BigInteger, BigInteger>();
            Storage.Put(mCKeeperPubKeysPrefix, bookKeeper.keepers.Serialize());
            InitGenesisBlockEvent(header.height, rawHeader);
            return true;
        }

        private static BookKeeper verifyPubkey(byte[] pubKeyList)
        {
            if (pubKeyList.Length % MCCHAIN_PUBKEY_LEN != 0)
            {
                Runtime.Notify("pubKeyList length illegal");
                throw new ArgumentOutOfRangeException();
            }
            int n = pubKeyList.Length / MCCHAIN_PUBKEY_LEN;
            int m = n - (n - 1) / 3;

            return getBookKeeper(n, m, pubKeyList);
        }

        private static BookKeeper getBookKeeper(int keyLength, int m, byte[] pubKeyList)
        {
            byte[] buff = new byte[] { };
            buff = WriteUint16(keyLength, buff);

            byte[][] keepers = new byte[keyLength][];

            for (int i = 0; i < keyLength; i++)
            {
                buff = WriteVarBytes(buff, compressMCPubKey(pubKeyList.Range(i * MCCHAIN_PUBKEY_LEN, MCCHAIN_PUBKEY_LEN)));
                byte[] hash = bytesToBytes32(SmartContract.Sha256((pubKeyList.Range(i * MCCHAIN_PUBKEY_LEN, MCCHAIN_PUBKEY_LEN).Range(3, 64))));
                keepers[i] = hash;
            }
            BookKeeper bookKeeper = new BookKeeper();

            buff = WriteUint16(m, buff);
            bookKeeper.nextBookKeeper = bytesToBytes20(Hash160(buff));
            bookKeeper.keepers = keepers;
            return bookKeeper;
        }

        private static byte[] compressMCPubKey(byte[] key)
        {
            if (key.Length < 34) return key;
            int index = 2;
            byte a = 0x02;
            byte b = 0x03;
            byte[] newkey = key.Range(0, 35);
            byte[] point = key.Range(66, 1);
            if (point.AsBigInteger() % 2 == 0)
            {
                newkey[index] = a;
            }
            else
            {
                newkey[index] = b;
            }
            return newkey;
        }

        private static bool verifySig(byte[] rawHeader, byte[] signList, object[] keepers, int m)
        {
            byte[] hash = SmartContract.Hash256(rawHeader);
            Runtime.Notify(hash);
            int signed = 0;
            for (int i = 0; i < signList.Length / MCCHAIN_SIGNATURE_LEN; i++)
            {
                byte[] r = (signList.Range(i * MCCHAIN_SIGNATURE_LEN, 32));
                byte[] s = (signList.Range(i * MCCHAIN_SIGNATURE_LEN + 32, 32));
                int index = i * MCCHAIN_SIGNATURE_LEN + 64;
                BigInteger v = signList.Range(index, 1).ToBigInteger();
                byte[] signer;
                if (v == 1)
                {
                    signer = SmartContract.Sha256(Secp256k1Recover(r, s, false, SmartContract.Sha256(hash)));
                }
                else
                {
                    signer = SmartContract.Sha256(Secp256k1Recover(r, s, true, SmartContract.Sha256(hash)));
                }
                if (containsAddress(keepers, signer))
                {
                    signed += 1;
                }
            }
            Runtime.Notify(signed);
            return signed >= m;
        }

        private static object verifySigTest(byte[] rawHeader, byte[] signList)
        {
            byte[] hash = SmartContract.Hash256(rawHeader);
            Runtime.Notify(hash);
            int signed = 0;
            object[] result = new object[5];
            for (int i = 0; i < signList.Length / MCCHAIN_SIGNATURE_LEN; i++)
            {
                byte[] r = (signList.Range(i * MCCHAIN_SIGNATURE_LEN, 32));
                byte[] s = (signList.Range(i * MCCHAIN_SIGNATURE_LEN + 32, 32));
                int index = i * MCCHAIN_SIGNATURE_LEN + 64;
                BigInteger v = signList.Range(index, 1).ToBigInteger();
                byte[] signer;
                if (v == 1)
                {
                    signer = SmartContract.Sha256(Secp256k1Recover(r, s, false, SmartContract.Sha256(hash)));
                }
                else
                {
                    signer = SmartContract.Sha256(Secp256k1Recover(r, s, true, SmartContract.Sha256(hash)));
                }
                result[i] = signer;
            }
            Runtime.Notify(signed);
            return result;
        }

        private static bool containsAddress(object[] keepers, byte[] pubkey)
        {
            for (int i = 0; i < keepers.Length; i++)
            {
                if (keepers[i].Equals(pubkey))
                {
                    return true;
                }
            }
            return false;
        }

        private static BigInteger IsGenesised()
        {
            return Storage.Get("IsInitGenesisBlock").AsBigInteger();
        }

        private static Header deserializHeader(byte[] Source)
        {
            Header header = new Header();
            int offset = 0;
            //get version
            header.version = Source.Range(offset, 4).ToBigInteger();
            offset += 4;
            //get chainID
            header.chainId = Source.Range(offset, 8).ToBigInteger();
            offset += 8;
            //get prevBlockHash, Hash
            header.prevBlockHash = ReadHash(Source, offset);
            offset += 32;
            //get transactionRoot, Hash
            header.transactionRoot = ReadHash(Source, offset);
            offset += 32;
            //get crossStatesRoot, Hash
            header.crossStatesRoot = ReadHash(Source, offset);
            offset += 32;
            //get blockRoot, Hash
            header.blockRoot = ReadHash(Source, offset);
            offset += 32;
            //get timeStamp,uint32
            header.timeStamp = Source.Range(offset, 4).ToBigInteger();
            offset += 4;
            //get height
            header.height = Source.Range(offset, 4).ToBigInteger();
            offset += 4;
            //get consensusData
            header.ConsensusData = Source.Range(offset, 8).ToBigInteger();
            offset += 8;
            //get consensysPayload
            var temp = ReadVarBytes(Source, offset);
            header.consensusPayload = (byte[])temp[0];
            offset = (int)temp[1];
            //get nextBookKeeper
            header.nextBookKeeper = Source.Range(offset, 20);

            return header;
        }

        private static bool ExecuteCrossChainTx(ToMerkleValue value)
        {
            if (value.TxParam.toContract.Length == 20)
            {
                DyncCall TargetContract = (DyncCall)value.TxParam.toContract.ToDelegate();
                object[] parameter = new object[] { value.TxParam.args, value.TxParam.fromContract, value.fromChainID };
                if (TargetContract(value.TxParam.method.AsString(), parameter) is null)
                {
                    return false;
                }
                else
                {
                    Storage.Put(transactionPrefix.Concat(value.fromChainID.AsByteArray()).Concat(value.txHash), 1);
                    return true;
                }
            }
            else
            {
                Runtime.Notify("Contract length is not correct");
                return false;
            }
        }

        private static BigInteger getRequestID(BigInteger chainID)
        {
            byte[] requestID = Storage.Get(requestIDPrefix.Concat(chainID.ToByteArray()));
            if (requestID != null)
            {
                return requestID.AsBigInteger();
            }
            return 0;
        }

        private static byte[] putRequest(BigInteger chainID, BigInteger requestID, CrossChainTxParameter para)
        {
            requestID = requestID + 1;
            byte[] requestKey = requestPreifx.Concat(chainID.ToByteArray()).Concat(requestID.ToByteArray());
            Storage.Put(requestKey, WriteCrossChainTxParameter(para));
            Storage.Put(requestIDPrefix.Concat(chainID.ToByteArray()), requestID);
            return requestKey;
        }

        private static ToMerkleValue deserializMerkleValue(byte[] Source)
        {
            ToMerkleValue result = new ToMerkleValue();
            int offset = 0;

            //get txHash
            var temp = ReadVarBytes(Source, offset);
            result.txHash = (byte[])temp[0];
            offset = (int)temp[1];

            //get fromChainID, Uint64
            result.fromChainID = Source.Range(offset, 8).ToBigInteger();
            offset = offset + 8;

            //get CrossChainTxParameter
            result.TxParam = deserializCrossChainTxParameter(Source, offset);
            return result;
        }

        private static CrossChainTxParameter deserializCrossChainTxParameter(byte[] Source, int offset)
        {
            CrossChainTxParameter txParameter = new CrossChainTxParameter();
            //get txHash
            var temp = ReadVarBytes(Source, offset);
            txParameter.txHash = (byte[])temp[0];
            offset = (int)temp[1];

            //get crossChainId
            temp = ReadVarBytes(Source, offset);
            txParameter.crossChainID = (byte[])temp[0];
            offset = (int)temp[1];

            //get fromContract
            temp = ReadVarBytes(Source, offset);
            txParameter.fromContract = (byte[])temp[0];
            offset = (int)temp[1];

            //get toChainID
            txParameter.toChainID = Source.Range(offset, 8).ToBigInteger();
            offset = offset + 8;

            //get toContract
            temp = ReadVarBytes(Source, offset);
            txParameter.toContract = (byte[])temp[0];
            offset = (int)temp[1];

            //get method
            temp = ReadVarBytes(Source, offset);
            txParameter.method = (byte[])temp[0];
            offset = (int)temp[1];

            //get params
            temp = ReadVarBytes(Source, offset);
            txParameter.args = (byte[])temp[0];
            offset = (int)temp[1];

            return txParameter;
        }

        private static byte[] WriteCrossChainTxParameter(CrossChainTxParameter para)
        {
            byte[] result = new byte[] { };
            result = WriteVarBytes(result, para.txHash);
            result = WriteVarBytes(result, para.crossChainID);
            result = WriteVarBytes(result, para.fromContract);
            byte[] toChainIDBytes = PadRight(para.toChainID.AsByteArray(), 8);
            result = result.Concat(toChainIDBytes);
            result = WriteVarBytes(result, para.toContract);
            result = WriteVarBytes(result, para.method);
            result = WriteVarBytes(result, para.args);
            return result;
        }

        private static byte[] MerkleProve(byte[] path, byte[] root)
        {
            int offSet = 0;
            var temp = ReadVarBytes(path, offSet);
            byte[] value = (byte[])temp[0];
            offSet = (int)temp[1];
            byte[] hash = HashLeaf(value);
            int size = (path.Length - offSet) / 32;
            for (int i = 0; i < size; i++)
            {
                var f = ReadBytes(path, offSet, 1);
                offSet = (int)f[1];

                var v = ReadBytes(path, offSet, 32);
                offSet = (int)v[1];
                if ((byte[])f[0] == new byte[] { 0 })
                {
                    hash = HashChildren((byte[])v[0], hash);
                }
                else
                {
                    hash = HashChildren(hash, (byte[])v[0]);
                }
            }
            if (hash.Equals(root))
            {
                return value;
            }
            else
            {
                return new byte[] { 0x00 };
            }
        }

        private static byte[] HashChildren(byte[] v, byte[] hash)
        {
            byte[] prefix = { 1 };
            return SmartContract.Sha256(prefix.Concat(v).Concat(hash));
        }

        private static byte[] HashLeaf(byte[] value)
        {
            byte[] prefix = { 0x00 };
            return SmartContract.Sha256(prefix.Concat(value));
        }

        private static byte[] WriteUint16(BigInteger value, byte[] Source)
        {
            return Source.Concat(PadRight(value.ToByteArray(), 2));
        }

        private static byte[] WriteVarBytes(byte[] Source, byte[] Target)
        {
            return WriteVarInt(Target.Length, Source).Concat(Target);
        }

        private static byte[] WriteVarInt(BigInteger value, byte[] source)
        {
            if (value < 0)
            {
                return source;
            }
            else if (value < 0xFD)
            {
                var v = PadRight(value.ToByteArray(), 1);
                return source.Concat(v);
            }
            else if (value <= 0xFFFF) // 0xff, need to pad 1 0x00
            {
                byte[] length = new byte[] { 0xFD };
                var v = PadRight(value.ToByteArray(), 2);
                return source.Concat(length).Concat(v);
            }
            else if (value <= 0XFFFFFFFF) //0xffffff, need to pad 1 0x00 
            {
                byte[] length = new byte[] { 0xFE };
                var v = PadRight(value.ToByteArray(), 4);
                return source.Concat(length).Concat(v);
            }
            else //0x ff ff ff ff ff, need to pad 3 0x00
            {
                byte[] length = new byte[] { 0xFF };
                var v = PadRight(value.ToByteArray(), 8);
                return source.Concat(length).Concat(v);
            }
        }

        private static byte[] ReadHash(byte[] Source, int offset)
        {
            if (offset + 32 <= Source.Length)
            {
                return Source.Range(offset, 32);
            }
            throw new ArgumentOutOfRangeException();
        }

        private static object[] ReadVarInt(byte[] buffer, int offset)
        {
            var res = ReadBytes(buffer, offset, 1); // read the first byte
            var fb = (byte[])res[0];
            if (fb.Length != 1) throw new ArgumentOutOfRangeException();
            var newOffset = (int)res[1];
            if (fb.Equals(new byte[] { 0xFD }))
            {
                return new object[] { buffer.Range(newOffset, 2).ToBigInteger(), newOffset + 2 };
            }
            else if (fb.Equals(new byte[] { 0xFE }))
            {
                return new object[] { buffer.Range(newOffset, 4).ToBigInteger(), newOffset + 4 };
            }
            else if (fb.Equals(new byte[] { 0xFF }))
            {
                return new object[] { buffer.Range(newOffset, 8).ToBigInteger(), newOffset + 8 };
            }
            else
            {
                return new object[] { fb.Concat(new byte[] { 0x00 }).ToBigInteger(), newOffset };
            }
        }

        private static object[] ReadVarBytes(byte[] buffer, int offset)
        {
            var res = ReadVarInt(buffer, offset);
            var count = (int)res[0];
            var newOffset = (int)res[1];
            return ReadBytes(buffer, newOffset, count);
        }

        private static object[] ReadBytes(byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException();
            return new object[] { buffer.Range(offset, count), offset + count };
        }

        private static byte[] PadRight(byte[] value, int length)
        {
            var l = value.Length;
            if (l > length)
            {
                value = value.Take(length);
            }
            for (int i = 0; i < length - l; i++)
            {
                value = value.Concat(new byte[] { 0x00 });
            }
            return value;
        }

        private static byte[] bytesToBytes20(byte[] Source)
        {
            if (Source.Length != 20)
            {
                throw new ArgumentOutOfRangeException();
            }
            else
            {
                return Source;
            }
        }

        private static byte[] bytesToBytes32(byte[] Source)
        {
            if (Source.Length != 32)
            {
                throw new ArgumentOutOfRangeException();
            }
            else
            {
                return Source;
            }
        }

        [Syscall("Neo.Cryptography.Secp256k1Recover")]
        public static extern byte[] Secp256k1Recover(byte[] r, byte[] s, bool v, byte[] message);
    }
    public struct ToMerkleValue
    {
        public byte[] txHash;
        public BigInteger fromChainID;
        public CrossChainTxParameter TxParam;
    }

    public struct CrossChainTxParameter
    {
        public BigInteger toChainID;
        public byte[] toContract;
        public byte[] method;
        public byte[] args;

        public byte[] txHash;
        public byte[] crossChainID;
        public byte[] fromContract;
    }

    public struct Header
    {
        public BigInteger version;//uint32
        public BigInteger chainId;//uint64
        public byte[] prevBlockHash;//Hash
        public byte[] transactionRoot;//Hash  无用
        public byte[] crossStatesRoot;//Hash  用来验证跨链交易
        public byte[] blockRoot;//Hash 用来验证header
        public BigInteger timeStamp;//uint32
        public BigInteger height;//uint32
        public BigInteger ConsensusData;//uint64
        public byte[] consensusPayload;
        public byte[] nextBookKeeper;
    }

    public struct BookKeeper
    {
        public byte[] nextBookKeeper;
        public byte[][] keepers;
    }
}
