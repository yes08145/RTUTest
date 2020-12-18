namespace TCPSocketCl
{
    public class ResultSet
    {
        public string strHexSplit;
        public string hex_cksum;
        public int resultSet;
        public ResultSet(string strHexSplit, string hex_cksum, int resultSet)
        {
            this.strHexSplit = strHexSplit;
            this.hex_cksum = hex_cksum;
            this.resultSet = resultSet;
        }
    }
}