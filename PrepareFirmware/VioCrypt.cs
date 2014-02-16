
namespace PrepareFirmware
{
    public class VioCrypt
    {
        private readonly byte[] _bufferForInternalUse;
        private byte[] _cryptTable;
        private byte[] _decryptTable;


        public byte[] CryptTable {
            get { return _cryptTable; }
            set { _cryptTable = CheckTableForCorrectData(value) ? value : null; }   
        }

        public byte[] DecryptTable {
            get { return _decryptTable; }
            set { _decryptTable = CheckTableForCorrectData(value) ? value : null; }   
        }

        private byte _cryptPointer;
        private byte _decryptPointer;
        public VioCrypt() {
            ResetCryptState();
            CryptTable = null;
            DecryptTable = null;
            _bufferForInternalUse = new byte[0x100];
        }

        private bool CheckTableForCorrectData(byte[] tableBytes)
        {
            if (tableBytes == null) return false;
            if (tableBytes.Length != 0x100) return false;
            CleanInternalBuffer();
            foreach (var idx in tableBytes)
            {
                ++_bufferForInternalUse[idx];
                if (_bufferForInternalUse[idx] == 1) continue;
                return false;
            }
            return true;
        }

        public void ResetCryptState() {
            _cryptPointer = 0x00;
            _decryptPointer = 0x00;
        }

        private void CleanInternalBuffer() {
            for (var idx = 0; idx < _bufferForInternalUse.Length; ++idx) _bufferForInternalUse[idx] = 0x00;
        }

        public byte[] ContinueCrypt(byte[] bufferToCrypt, int lenght = 0) {
            if (CryptTable == null) return null;
            if (lenght == 0) lenght = bufferToCrypt.Length;
            var retVal = new byte[lenght];

            for (var i = 0; i < lenght; i++)
            {
                var tmp = bufferToCrypt[i];
                tmp = _cryptTable[_cryptPointer ^ tmp];
                tmp -= _cryptPointer;
                _cryptPointer += tmp;
                retVal[i] = tmp;
            }
            return retVal;
        }

        public byte[] ContinueDecrypt(byte[] bufferToDecrypt , int lenght = 0) {
            if (DecryptTable == null) return null;
            if (lenght == 0) lenght = bufferToDecrypt.Length;
            var retVal = new byte[lenght];

            for (byte i = 0; i < lenght; i++)
            {
                var tmp = _decryptPointer;
                _decryptPointer += bufferToDecrypt[i];
                var tmp2 = tmp;
                tmp = DecryptTable[_decryptPointer];
                tmp ^= tmp2;
                retVal[i] = tmp;
            }
            return retVal;
        }
    }
}