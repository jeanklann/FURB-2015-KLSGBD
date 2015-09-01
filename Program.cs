using System;
using System.Collections.Generic;
using System.IO;


namespace ProjetoBD2 {
    static class Constants{
        public const string FILEPATH = "/home/klann/projetobd2/prjbd2.data";
        public const int MAXPAGES = 20;
        public const int MAXMEMORYPAGES = 10;
        public const int PAGESIZE = 256;
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

            p[0].Data = new byte[Constants.PAGESIZE];
            p[0].Insert(new object[]{ 13, "teste", "testando" });
            p[0].Insert(new object[]{ 14, "outro", "ttteeste" });
            p[0].Insert(new object[]{ 15, "mais", "umteste" });
            p.Save(0);
            p[0].Print();

            object[] tmp = new object[]{ 0, "", "" };
            object[] tmp2 = p[0].Read(2, tmp);

            Console.WriteLine(tmp2[0]);
            Console.WriteLine(tmp2[1]);
            Console.WriteLine(tmp2[2]);

            //p[0].Print();

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
                page.Data[i] = buffer[i];
            }
            page.Dirt = false;
            page.PinCount=1;
            page.UltimoAcesso = DateTime.Now.ToFileTimeUtc();
        }
    }

    class Page{
        public byte[] Data = new byte[Constants.PAGESIZE];
        public bool Dirt = false;
        public int PinCount = 0;
        public long UltimoAcesso = 0;
        public int FileIndex;



        public void Insert(string data){
            Dirt = true;
            int qtde = BitConverter.ToInt32(Data, Data.Length - 1 * sizeof(int));
            int offset = BitConverter.ToInt32(Data, Data.Length - 3 * sizeof(int));

            Array.Copy(Data, Data.Length - 3 * sizeof(int), Data, (Data.Length - 4 * sizeof(int)) - qtde*sizeof(int), sizeof(int));
            Array.Copy(BitConverter.GetBytes(offset + data.Length), 0, Data, Data.Length - 3 * sizeof(int), sizeof(int));

            Array.Copy(Functions.GetBytes(data), 0, Data, offset, data.Length);
            qtde++;
            Array.Copy(BitConverter.GetBytes(qtde), 0, Data, Data.Length - 1 * sizeof(int), sizeof(int));
            PinCount++;
            UltimoAcesso = DateTime.Now.ToFileTimeUtc();
        }
            



        public object[] Read(int i, object[] structure){
            object[] values = new object[structure.Length];
            int qtde = BitConverter.ToInt32(Data, Data.Length - 1 * sizeof(int));
            if(structure==null) 
                throw new ArgumentException("Deve ser enviado a estrutura como parâmetro");
            if(i > qtde)
                throw new ArgumentException("Indice fora de alcance");
            int offset = BitConverter.ToInt32(Data, (Data.Length - 4*sizeof(int))-i*sizeof(int));
            for (int j = 0; j < structure.Length; j++) {
                if(structure[j] is int) {
                    int offsetInt = BitConverter.ToInt32(Data, offset+j*sizeof(int));
                    values[j] = BitConverter.ToInt32(Data, offset + offsetInt);
                } else if(structure[j] is string) {
                    int offsetString = BitConverter.ToInt32(Data, offset+j*sizeof(int));
                    int sizeString = BitConverter.ToInt32(Data, offset+offsetString);
                    byte[] arr = new byte[sizeString];
                    Array.Copy(Data, offset + offsetString + sizeof(int), arr, 0, sizeString);
                    values[j] = Functions.GetString(arr);
                }
            }
            return values;

        }

        public void Insert(object[] data){
            int totalSize = data.Length*sizeof(int);
            foreach(object item in data) {
                if(item is string) {
                    totalSize += ((string)item).Length+1*sizeof(int);
                } if(item is int) {
                    totalSize += sizeof(int);
                }
            }

            byte[] newData = new byte[totalSize];
            int offset = data.Length*sizeof(int);
            int j = 0;
            foreach(object item in data) {
                Array.Copy(BitConverter.GetBytes(offset), 0, newData, j*sizeof(int), sizeof(int));
                if(item is int) {
                    Array.Copy(BitConverter.GetBytes((int)item), 0, newData, offset, sizeof(int));
                    offset += sizeof(int);
                } else if(item is string) {
                    Array.Copy(BitConverter.GetBytes(((string)item).Length), 0, newData, offset, sizeof(int));
                    Array.Copy(Functions.GetBytes((string)item), 0, newData, offset + 1*sizeof(int), ((string)item).Length);
                    offset += ((string)item).Length+1*sizeof(int);
                }
                j++;
            }
            Insert(Functions.GetString(newData));
        }
        public void ChangePage(byte[] data){
            Data = data;
            Dirt = true;
            PinCount++;
            UltimoAcesso = DateTime.Now.ToFileTimeUtc();
        }
        public void ChangePage(string data){
            
            Array.Copy(Functions.GetBytes(data), 0, Data, 0, data.Length);
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
                    Console.Write(((char)Data[i])+" ");
                }
                Console.Write(" ");
            }
            Console.WriteLine();
            Console.WriteLine("==================================================");
        }
    }

}
