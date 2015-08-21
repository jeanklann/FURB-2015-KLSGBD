using System;
using System.Collections.Generic;
using System.IO;


namespace ProjetoBD2 {
    static class Constants{
        public const string FILEPATH = "/home/klann/projetobd2/prjbd2.data";
        public const int MAXPAGES = 20;
        public const int MAXMEMORYPAGES = 10;
        public const int PAGESIZE = 128;
    }

    class MainClass {
        public static void Main(string[] args) {


            Console.WriteLine("");
            PageInterface p = new PageInterface(PoliticaDeSubstituicao.MRU);


            /*
            p[0].Insert("teste");
            p[0].Insert("ehauhea");
            p[0].Insert("kakaroto");

            p.Save(0);
            */
            p[0].Data = new char[128];
            p[0].Insert(new object[]{ 13, "teste", "testando" });
            p[0].Print();

            //p[0].Data = new char[128];
            //p.Load(0);
            //p.Print();

        }
    }
    enum PoliticaDeSubstituicao {
        LRU, MRU, RANDOM
    }
    class PageInterface{
        private PoliticaDeSubstituicao politica = PoliticaDeSubstituicao.LRU;
        private List<Page> Pages = new List<Page>();
        private List<Page> MRU = new List<Page>();
        private Random Random = new Random();
        private readonly FileStream stream;

        public PageInterface(PoliticaDeSubstituicao Politica){
            politica = Politica;
            stream = new FileStream(Constants.FILEPATH, FileMode.OpenOrCreate);
            if(stream.Length != Constants.PAGESIZE * Constants.MAXPAGES) {
                Console.WriteLine("Os dados do arquivo sao invalidos, foram encontrador " + stream.Length + " bytes, resetando arquivo.");
                byte[] tmp = new byte[Constants.PAGESIZE * Constants.MAXPAGES];
                stream.Write(tmp, 0, tmp.Length);
                stream.Position = 0;
            }

        }


        public void Print(){
            for(int i = 0; i < Pages.Count; i++) {
                Console.WriteLine("*********************************");
                Console.WriteLine("Pagina " + i);
                Pages[i].Print();
            }
        }

        private Page Refresh(Page page){
            MRU.Remove(page);
            MRU.Add(page);
            return page;
        }

        public Page this[int index]{
            get{
                if(index > Constants.MAXPAGES-1)
                    throw new IndexOutOfRangeException("O numero maximo de paginas no disco eh "+Constants.MAXPAGES);
                Page page = LocatePageIndex(index);
                if(page == null) {
                    if(MRU.Count == Constants.MAXMEMORYPAGES) {
                        if(politica == PoliticaDeSubstituicao.LRU)
                            MRU.Remove(GetLastRecentlyUsed());
                        else if(politica == PoliticaDeSubstituicao.LRU)
                            MRU.Remove(GetMostRecentlyUsed());
                        else
                            MRU.Remove(GetRandomPage());
                    }
                    page = new Page();
                    page.FileIndex = index;
                    Pages.Add(page);
                    Load(page);

                }
                return Refresh(page);
            }
        }

        private Page GetMostRecentlyUsed(){
            return Refresh(MRU[MRU.Count - 1]);
        }

        private Page GetLastRecentlyUsed(){
            return Refresh(MRU[0]);
        }

        private Page GetRandomPage(){
            return Refresh(MRU[(int)(Random.NextDouble() * MRU.Count)]);
        }

        public void Save(Page page){
            byte[] buffer = new byte[Constants.PAGESIZE];
            for(int i = 0; i < buffer.Length; i++) {
                buffer[i] = (byte) page.Data[i];
            }
            stream.Position = page.FileIndex * Constants.PAGESIZE;
            stream.Write(buffer, 0, Constants.PAGESIZE);

            page.PinCount++;
            page.UltimoAcesso = DateTime.Now.ToFileTimeUtc();
        }
        private Page LocatePageIndex(int index){
            foreach(var page in Pages) {
                if(page.FileIndex == index) {
                    return page;
                }
            }
            return null;
        }

        public void Save(int index){
            Page page = LocatePageIndex(index);
            Save(page);
        }

        private void Load(Page page){

            byte[] buffer = new byte[Constants.PAGESIZE];
            stream.Position = page.FileIndex * Constants.PAGESIZE;
            stream.Read(buffer, 0, Constants.PAGESIZE);
            for(int i = 0; i < buffer.Length; i++) {
                page.Data[i] = (char) buffer[i];
            }
            page.Dirt = false;
            page.PinCount=1;
            page.UltimoAcesso = DateTime.Now.ToFileTimeUtc();
        }
    }

    class Page{
        public char[] Data = new char[Constants.PAGESIZE];
        public bool Dirt = false;
        public int PinCount = 0;
        public long UltimoAcesso = 0;
        public int FileIndex;

        public void Insert(string data){
            Dirt = true;
            int qtde = Data[Data.Length - 1];
            int offset = 0;
            int i = 0;
            for(; i < qtde; i++) {
                offset += Data[(Data.Length - 3) - i];
            }
            Data[(Data.Length - 3) - i] = (char) data.Length;
            data.CopyTo(0, Data, offset, data.Length);
            Data[Data.Length - 1]++;

            PinCount++;
            UltimoAcesso = DateTime.Now.ToFileTimeUtc();
        }
        public void Insert(object[] data){
            int totalSize = data.Length;
            foreach(object item in data) {
                if(item is string) {
                    totalSize += ((string)item).Length+1;
                } if(item is int) {
                    totalSize += sizeof(int);
                }
            }
            char[] newData = new char[totalSize];
            int offset = data.Length;
            int j = 0;
            foreach(object item in data) {
                newData[j] = (char)offset;
                if(item is int) {
                    string bin = Convert.ToString((int)item, 2);
                    while(bin.Length < sizeof(int) * 8)
                        bin = "0" + bin;
                    for(int i = 0; i < sizeof(int); i++) {
                        byte b = (byte)Convert.ToInt32(bin.Substring(i * 8, 8), 2);
                        newData[offset] = (char)b;
                        offset++;
                    }
                } else if(item is string) {
                    newData[offset] = (char) ((string)item).Length;
                    ((string)item).CopyTo(0, newData, offset+1, ((string)item).Length);
                    offset += ((string)item).Length+1;
                }
                j++;
            }
            Insert(new string(newData));
        }
        public void ChangePage(char[] data){
            Data = data;
            Dirt = true;
            PinCount++;
            UltimoAcesso = DateTime.Now.ToFileTimeUtc();
        }
        public void ChangePage(string data){
            data.CopyTo(0,Data,0,data.Length);
            Dirt = true;
            PinCount++;
            UltimoAcesso = DateTime.Now.ToFileTimeUtc();
        }
        public void Print(){
            PinCount++;
            UltimoAcesso = DateTime.Now.ToFileTimeUtc();
            Console.WriteLine("A pagina " + (Dirt ? "" : "nao ") + "esta suja");
            Console.WriteLine("Pin count: " + PinCount);
            Console.WriteLine("Ultimo acesso: " + UltimoAcesso);
            Console.WriteLine("Dados:");
            ShowData();
        }

        private void ShowData(){
            Console.WriteLine("==================================================");
            Console.WriteLine("   0  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15");
            for(int i = 0; i < Data.Length; i++) {
                if(i % 16 == 0) {
                    if(i!=0)
                        Console.WriteLine();
                    Console.Write(i / 16+" "+(i/16<10?" ":""));
                }

                if(Data[i] < 32) {
                    Console.Write((int)Data[i]);
                    if(Data[i] < 10)
                        Console.Write(" ");
                } else {
                    Console.Write(Data[i]+" ");
                }
                Console.Write(" ");
            }
            Console.WriteLine();
            Console.WriteLine("==================================================");
        }
    }
}
