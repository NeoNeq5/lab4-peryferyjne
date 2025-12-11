using PCSC;
using PCSC.Iso7816;
using System;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Text;

class Program
{
    static void Main()
    {
        var contextFactory = ContextFactory.Instance;
        var ctx = contextFactory.Establish(SCardScope.System);
        var iso = new IsoReader(ctx, "ACME Smartcard reader", SCardShareMode.Shared, SCardProtocol.Any, false);
        while (true)
        {
            Console.WriteLine("Co chcesz zrobić?");
            string co = Console.ReadLine();
            switch(co) 
            {
              case "czytnik":
                break;
              case "sms":
                break;
              default:
                Console.Write("Dostępne opcje to: \n -wypisanie sms, komenda 'sms' \n -wybór czytnika, komenda 'czytnik' \n");
                break;
            }
        }
    }
    void show_phonebook(IsoReader iso)
    {
        byte[] data={0x3F, 0x00};
        send_APDU(iso,0x00,0xA4,0x00,0x00,data);
        data=new byte[] {0x7F, 0x10};
        send_APDU(iso,0x00,0xA4,0x00,0x00,data);
        data=new byte[] {0x6F, 0x3A};
        Response resp =send_APDU(iso,0x00,0xA4,0x00,0x00,data);
        if (resp.SW1 == 0x9F || resp.SW1 == 0x61)
        {   
            resp=send_APDU(iso,0x00,0xC0,0x00,0x00,null,resp.SW2);
        }
        var inf = resp.GetData();
        int len=inf[14];
        int count=inf[15];
        for (int i=1; i <= count; i++)
        {
            resp=send_APDU(iso,0x00,0xB2,(byte)i,0x04,null,(byte)len);
            var dane=resp.GetData();
            int end = Array.IndexOf(dane, (byte)0xFF);
            if (end < 0) end = dane.Length;
            var r = dane.Take(end).ToArray();
            string name = Encoding.ASCII.GetString(r).Trim();
            int lenPos = len - 14;
            int lengthNumber = dane[lenPos];
            string number = "-";
            if (lengthNumber != 0xFF && lengthNumber != 0x00)
            {
                int tonNpiPos = lenPos + 1;
                int numStart = lenPos + 2;
                var bcd = dane.Skip(numStart).Take(lengthNumber).ToArray();
                var sb = new StringBuilder();
                foreach (var b in bcd)
                {
                  int lo = b & 0x0F;
                  int hi = (b >> 4) & 0x0F;
                  if (lo <= 9) sb.Append(lo);
                  if (hi != 0xF && hi <= 9) sb.Append(hi);
                }
                number = sb.ToString();
            } 
            Console.WriteLine("Nazwa kontaktu: "+name+" Numer: "+number);
        }
    }
    Response send_APDU(IsoReader iso, byte a, byte b, byte c, byte d, byte[] arr, byte? e=null)
    {
        CommandApdu apdu;

        if (arr != null && arr.Length > 0)
        {
           apdu = new CommandApdu(IsoCase.Case4Short, iso.ActiveProtocol)
           {
              CLA = a,
              INS = b,
              P1 = c,
              P2 = d,
              Data = arr,
              Le = e ?? 0x00
           };
        }
        else
        {
           apdu = new CommandApdu(IsoCase.Case2Short, iso.ActiveProtocol)
           {
              CLA = a,
              INS = b,
              P1 = c,
              P2 = d,
              Le = e ?? 0x00
           };
        }
        var response = iso.Transmit(apdu);
        return response;
    }
    void read_sms()
    {
        
    }

}
 

