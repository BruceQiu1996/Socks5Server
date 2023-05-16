using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace Socks5Common
{
    public class Socks5ByteUtil
    {
        /// <summary>
        /// 获取数据包中的目标ip类型，ip地址，端口数据,所占用长度
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public (Socks5AddressType, IPAddress, ushort, int) GetProxyInfo(Memory<byte> data)
        {
            IPAddress targetAddress = null;
            ushort targetPort = 0;
            int addressLength = 0;
            Socks5AddressType addressType = (Socks5AddressType)data.Span[0];
            if (addressType == Socks5AddressType.IPV4)
            {
                targetAddress = new IPAddress(data.Span.Slice(1, 4));
                targetPort = BinaryPrimitives.ReadUInt16BigEndian(data.Span.Slice(5, 2));
                addressLength = 6;
            }
            else if (addressType == Socks5AddressType.Domain)
            {
                byte length = data.Span[1];
                targetAddress = Dns.GetHostEntry(Encoding.UTF8.GetString(data.Span.Slice(2, length))).AddressList[0];
                targetPort = BinaryPrimitives.ReadUInt16BigEndian(data.Span.Slice(2 + length, 2));
                addressLength = length + 1 + 2;
            }
            else if (addressType == Socks5AddressType.IPV6)
            {
                targetAddress = new IPAddress(data.Span.Slice(1, 16));
                targetPort = BinaryPrimitives.ReadUInt16BigEndian(data.Span.Slice(17, 2));
                addressLength = 18;
            }
            else
            {
                throw new NotSupportedException("NotSupported address type");
            }

            return (addressType, targetAddress, targetPort, addressLength);
        }
    }
}