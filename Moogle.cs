namespace MoogleEngine;
using System.IO;
public static class Moogle
{
    static string ruta_ejecucion = Directory.GetCurrentDirectory();
    static string ruta = ruta_ejecucion + "..\\..\\Content";
    static string[] archivos_txt = Directory.GetFiles(ruta, "*.txt");
    static Diccionario_Referencial Diccionario_R = new Diccionario_Referencial(archivos_txt);
    static Matriz_TF_IDF Matriz = new Matriz_TF_IDF(Diccionario_R, ruta);
    public static SearchResult Query(string query)
    {
        SearchItem[] items;
        Query_class quer = new Query_class(Diccionario_R, query);
        if (Query_class.Busqueda_valida(Diccionario_R, query))
        {
            Dictionary<string, double> Respuesta_query = Matriz.Respuesta_query(quer, Matriz, Diccionario_R);
            items = new SearchItem[Respuesta_query.Count];
            int i = 0;
            foreach (KeyValuePair<string, double> par in Respuesta_query)
            {
                items[i] = new SearchItem(par.Key.Substring(ruta.Length + 1), quer.Snippet(par.Key, Diccionario_R), (float)par.Value); i++;
            }
        }
        else
        {
            items = new SearchItem[1] { new SearchItem("Su busqueda es invalida", "escriba algo valido en el cuadro de texto", 0.9f), };
        }
        query = quer.Suggestion(Diccionario_R);
        return new SearchResult(items, query);
    }
}
public class Diccionario_Referencial
{
    public Dictionary<string, int> Ocurrencia_de_i_en_documentos = new Dictionary<string, int>();
    public int Cant_arch;
    public Dictionary<string, int> Indexador_Columnas = new Dictionary<string, int>();
    public Dictionary<string, int> Cant_pal_DOC = new Dictionary<string, int>();
    public Dictionary<string, int> Indexador_Fila = new Dictionary<string, int>();
    public Diccionario_Referencial(string[] archivos_txt)
    {
        int contador_indexador_columna = 0;
        int contador_indexador_fila = 0;
        for (int i = 0; i < archivos_txt.Length; i++)
        {
            List<string> Marca = new List<string>();
            string[] x = Tokenizar_txt(archivos_txt[i]);
            if (x.Length > 0)
            {
                Cant_pal_DOC.Add(archivos_txt[i], x.Length);
                Indexador_Fila.Add(archivos_txt[i], contador_indexador_fila); contador_indexador_fila++;
                foreach (string palabra in x)
                {
                    if (!Indexador_Columnas.ContainsKey(palabra))
                    {
                        Ocurrencia_de_i_en_documentos.Add(palabra, 1);
                        Indexador_Columnas.Add(palabra, contador_indexador_columna); contador_indexador_columna++;
                        Marca.Add(palabra);
                    }
                    if (Ocurrencia_de_i_en_documentos.ContainsKey(palabra) && !(Marca.Contains(palabra)))
                    {
                        Ocurrencia_de_i_en_documentos[palabra]++; Marca.Add(palabra);
                    }
                }
            }
        }
        this.Cant_arch = archivos_txt.Length;
    }
    //FAlta constructor con deserializar json
    public static string[] Tokenizar_txt(string ruta_de_archivo)
    {
        string Contenido_Archivo = File.ReadAllText(ruta_de_archivo);
        string Documento_miniscula = Contenido_Archivo.ToLower();
        char[] Separadores = { ' ', ',', '.', ':', '\t', ';', '\n', '\r', '!', '<', '>', '/', '[', ']', '{', '}', '+', '|', '?', '-', '_', '#', '@', '$', '%', '^', '&', '*', '~', '`', '(', ')', '=' };
        string[] documento_tokenizado = Documento_miniscula.Split(Separadores, StringSplitOptions.RemoveEmptyEntries);
        return documento_tokenizado;
    }
    public static string[] Tokenizar_txt(string Contenido_Archivo, bool innecesario)
    {
        string Documento_miniscula = Contenido_Archivo.ToLower();
        char[] Separadores = { ' ', ',', '.', ':', '\t', ';', '\n', '\r', '!', '<', '>', '/', '[', ']', '{', '}', '+', '|', '?', '-', '_', '#', '@', '$', '%', '^', '&', '*', '~', '`', '(', ')', '=' };
        string[] documento_tokenizado = Documento_miniscula.Split(Separadores, StringSplitOptions.RemoveEmptyEntries);
        return documento_tokenizado;
    }
}
public class Vector
{
    public double[] Vector_TF_IDF;
    public Vector(Diccionario_Referencial DIC_REF, string archivo)
    {
        Vector_TF_IDF = new double[DIC_REF.Indexador_Columnas.Count];
        Dictionary<string, int> diccionario_Freq_i_en_J = DIC_REF.Indexador_Columnas.Keys.ToDictionary(k => k, k => default(int));
        foreach (string palabra in Diccionario_Referencial.Tokenizar_txt(archivo))
        {
            if (diccionario_Freq_i_en_J.ContainsKey(palabra))
            {
                diccionario_Freq_i_en_J[palabra]++;
            }
        }
        foreach (KeyValuePair<string, int> par in DIC_REF.Indexador_Columnas)
        {
            Vector_TF_IDF[par.Value] = Calculo_TF_IDF(diccionario_Freq_i_en_J[par.Key], DIC_REF.Cant_pal_DOC[archivo], DIC_REF.Cant_arch, DIC_REF.Ocurrencia_de_i_en_documentos[par.Key]);
        }
    }
    //FAlta constructor del vector con la deserializacion del json
    public Vector(double[] Consulta_query)
    {
        this.Vector_TF_IDF = Consulta_query;
    }
    public static double Calculo_TF_IDF(int Freq_i_en_j, int cant_palabras_en_j, int Num_Archivos, double Num_arch_con_i)
    {
        double TF = (Math.Log10(Freq_i_en_j + 1)) / (Math.Log10(cant_palabras_en_j + 000.1));
        double IDF = Math.Log10(1 + (Num_Archivos / (Num_arch_con_i + 1)));
        return TF * IDF;
    }
    public double Multiplicar_por_Vector(Vector x)
    {
        double Producto = 0;
        for (int i = 0; i < x.Vector_TF_IDF.Length; i++)
        {
            Producto = Producto + (x.Vector_TF_IDF[i] * this.Vector_TF_IDF[i]);
        }
        return Producto;
    }
    public double Norma()
    {
        return Math.Sqrt(this.Multiplicar_por_Vector(this));
    }
    public double Cosigno(Vector x)
    {
        return (this.Multiplicar_por_Vector(x)) / (this.Norma() * x.Norma());
    }
}
public class Matriz_TF_IDF
{
    Vector[] Matriz;
    public Matriz_TF_IDF(Diccionario_Referencial DIC_REF, string Carpeta_Documentos)
    {
        Matriz = new Vector[DIC_REF.Indexador_Fila.Count];
        int count_indexador = 0;
        foreach (var x in DIC_REF.Indexador_Fila)
        {
            Vector y = new Vector(DIC_REF, x.Key);
            Matriz[count_indexador] = y;
            count_indexador++;
        }
    }
    public Dictionary<string, double> Respuesta_query(Query_class query, Matriz_TF_IDF Matriz, Diccionario_Referencial DIC_REF)
    {
        Dictionary<string, double> Solution = new Dictionary<string, double>();
        foreach (KeyValuePair<string, int> par in DIC_REF.Indexador_Fila)
        {
            if (Solution.Count < 5) // cambiando este numero se varia la cantidad de respuestas a el query
            {
                Solution.Add(par.Key, query.Vector_Query.Cosigno(Matriz.Matriz[par.Value]));
            }
            else
            {
                Solution.Add(par.Key, query.Vector_Query.Cosigno(Matriz.Matriz[par.Value])); Solution.Remove(Menor(Solution));
            }
        }
        foreach (var x in Solution)
        {
            if (x.Value == 0) { Solution.Remove(x.Key); }
        }
        //ordenar el diccionario por los TValue
        Dictionary<string, double> orderedSolution = Solution.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        return orderedSolution;
    }
    public static string Menor(Dictionary<string, double> dic)
    {
        KeyValuePair<string, double> menor = new KeyValuePair<string, double>("quitar", 1.1);
        foreach (KeyValuePair<string, double> par in dic)
        {
            if (par.Value < menor.Value)
            {
                menor = new KeyValuePair<string, double>(par.Key, par.Value);
            }
        }
        return menor.Key;
    }
}
public class Query_class
{
    public double[] query_TF_IDF;
    public Vector Vector_Query;
    public string[] query_original;
    public Query_class(Diccionario_Referencial DIC_REF, string query)
    {

        string[] query_tokenizado = Diccionario_Referencial.Tokenizar_txt(query, false);
        query_TF_IDF = new double[DIC_REF.Indexador_Columnas.Count];
        foreach (string palabra in query_tokenizado)
        {
            if (DIC_REF.Indexador_Columnas.ContainsKey(palabra))
            {
                query_TF_IDF[DIC_REF.Indexador_Columnas[palabra]]++;
            }
        }
        foreach (string palabra in query_tokenizado)
        {
            if (DIC_REF.Indexador_Columnas.ContainsKey(palabra))
            {
                query_TF_IDF[DIC_REF.Indexador_Columnas[palabra]] = Vector.Calculo_TF_IDF((int)query_TF_IDF[DIC_REF.Indexador_Columnas[palabra]], query_tokenizado.Length + 1, DIC_REF.Cant_arch, DIC_REF.Ocurrencia_de_i_en_documentos[palabra]);
            }
        }
        this.query_original = query_tokenizado;
        this.Vector_Query = new Vector(query_TF_IDF);
    }
    public string Snippet(string Documento, Diccionario_Referencial DicREF)
    {
        string[] DOC = Diccionario_Referencial.Tokenizar_txt(Documento);
        int count = 0;
        List<string> palabrasvalidas = new List<string>();
        foreach (string palabra in query_original)
        {
            if (DicREF.Indexador_Columnas.ContainsKey(palabra))
            {
                palabrasvalidas.Add(palabra);
                count++;
            }
        }
        Tuple<string, double>[] PalRevDes = new Tuple<string, double>[count];
        count = 0;
        foreach (string palabra in palabrasvalidas)
        {
            PalRevDes[count] = new Tuple<string, double>(palabra, query_TF_IDF[DicREF.Indexador_Columnas[palabra]]); count++;
        }
        var orden_palabra = PalRevDes.OrderByDescending(tuple => tuple.Item2).ToArray();
        if (orden_palabra.Length == 0)
        {
            return Vecindad_Pal(DOC, 0);
        }
        else if (orden_palabra.Length == 1)
        {
            for (int i = 0; i < DOC.Length; i++)
            {
                if (orden_palabra[0].Item1 == DOC[i]) { return Vecindad_Pal(DOC, i); }
            }
            return Vecindad_Pal(DOC, 0);
        }
        else
        {
            int palrev1 = -1;
            int pos1 = 0;
            int palrev2 = -1;
            int pos2 = 0;
            for (int i = 0; i < orden_palabra.Length && palrev2 == -1; i++)
            {
                int posicion = Array.IndexOf(DOC, orden_palabra[i].Item1);
                if (posicion >= 0)
                {
                    if (palrev1 == -1)
                    {
                        palrev1 = i; pos1 = posicion;
                    }
                    else { palrev2 = i; pos2 = posicion; }
                }
            }
            if (palrev1 == -1)
            {
                return Vecindad_Pal(DOC, 0);
            }
            else if (palrev1 != -1 && palrev2 == -1)
            {
                return Vecindad_Pal(DOC, pos1);
            }
            else
            {
                List<int> list_mas_rev = new List<int>();
                List<int> list_seg_mas_rev = new List<int>();
                for (int i = 0; i < DOC.Length; i++)
                {
                    if (orden_palabra[palrev1].Item1 == DOC[i]) { list_mas_rev.Add(i); }
                    else if (orden_palabra[palrev2].Item1 == DOC[i]) { list_seg_mas_rev.Add(i); }
                }
                int[] posicion = PosMasCercana(list_mas_rev, list_seg_mas_rev);
                if (posicion[1] - posicion[0] > 7) { return Vecindad_Pal(DOC, posicion[0]) + " ... " + Vecindad_Pal(DOC, posicion[1]); }
                else { return Vecindad_Pal(DOC, posicion[0], posicion[1]); }
            }
        }
    }
    public string Vecindad_Pal(string[] Doc, int i)
    {
        string Vecindad = "";
        if (i >= 3 && i < Doc.Length - 3)
        {
            for (int j = i - 3; j < i + 4; j++) { Vecindad += Doc[j] + " "; }
            return Vecindad;
        }
        else if (i < 3 && i < Doc.Length - 3)
        {
            for (int j = 0; j < i + 4; j++) { Vecindad += Doc[j] + " "; }
            return Vecindad;
        }
        else if (i >= 3 && i >= Doc.Length - 3)
        {
            for (int j = i - 3; j < Doc.Length; j++) { Vecindad += Doc[j] + " "; }
            return Vecindad;
        }
        else if (i < 3 && i >= Doc.Length - 3)
        {
            for (int j = 0; j < Doc.Length; j++) { Vecindad += Doc[j] + " "; }
            return Vecindad;
        }
        else return Doc[i];
    }
    public string Vecindad_Pal(string[] Doc, int i, int i2)
    {
        string Vecindad = "";
        if (i >= 3 && i2 < Doc.Length - 3)
        {
            for (int j = i - 3; j < i2 + 4; j++) { Vecindad += Doc[j] + " "; }
            return Vecindad;
        }
        else if (i < 3 && i2 < Doc.Length - 3)
        {
            for (int j = 0; j < i2 + 4; j++) { Vecindad += Doc[j] + " "; }
            return Vecindad;
        }
        else if (i >= 3 && i2 >= Doc.Length - 3)
        {
            for (int j = i - 3; j < Doc.Length; j++) { Vecindad += Doc[j] + " "; }
            return Vecindad;
        }
        else if (i < 3 && i2 >= Doc.Length - 3)
        {
            for (int j = 0; j < Doc.Length; j++) { Vecindad += Doc[j] + " "; }
            return Vecindad;
        }
        else return Doc[i];
    }
    public int[] PosMasCercana(List<int> a, List<int> b)
    {
        int[] resultado = new int[2];
        int minMOd = int.MaxValue;
        foreach (int pos1 in a)
        {
            foreach (int pos2 in b)
            {
                int mod = Math.Abs(pos1 - pos2);
                if (mod == 1)
                {
                    if (pos1 < pos2) { resultado[0] = pos1; resultado[1] = pos2; }
                    else if (pos1 > pos2) { resultado[0] = pos2; resultado[1] = pos1; }
                    return resultado;
                }
                else if (mod < minMOd)
                {
                    minMOd = mod;
                    if (pos1 < pos2) { resultado[0] = pos1; resultado[1] = pos2; }
                    else if (pos1 > pos2) { resultado[0] = pos2; resultado[1] = pos1; }
                }
            }
        }
        return resultado;
    }
    public static bool Busqueda_valida(Diccionario_Referencial dic, string query)
    {
        foreach (string palabra in Diccionario_Referencial.Tokenizar_txt(query, false))
        {
            if (dic.Indexador_Columnas.ContainsKey(palabra)) { return true; }
        }
        return false;
    }
    public static int LevensteinsDistance(string word1, string word2)
    {
        int[,] Matriz = new int[word1.Length + 1, word2.Length + 1];
        for (int i = 0; i <= word1.Length; i++)
        {
            Matriz[i, 0] = i;
        }
        for (int i = 0; i <= word2.Length; i++)
        {
            Matriz[0, i] = i;
        }
        for (int i = 1; i <= word1.Length; i++)
        {
            for (int j = 1; j <= word2.Length; j++)
            {
                int igual = (word1[i - 1] == word2[j - 1]) ? 0 : 1;
                Matriz[i, j] = Math.Min(Math.Min(Matriz[i - 1, j] + 1, Matriz[i, j - 1] + 1), Matriz[i - 1, j - 1] + igual);
            }
        }
        return Matriz[word1.Length, word2.Length];
    }
    public static string MostSimilarWord(string Word, Diccionario_Referencial Dic_Ref)
    {
        string MostSimilarWord = ""; int Distance = int.MaxValue;
        foreach (var x in Dic_Ref.Indexador_Columnas)
        {
            int LevensteiDistance = Query_class.LevensteinsDistance(Word, x.Key);
            if (Distance == 1) { return MostSimilarWord; }
            else if (LevensteiDistance < Distance) { MostSimilarWord = x.Key; Distance = LevensteiDistance; }
        }
        return MostSimilarWord;
    }
    public string Suggestion(Diccionario_Referencial Dic_Ref)
    {
        for (int i = 0; i < this.query_original.Length; i++)
        {
            if (!Dic_Ref.Indexador_Columnas.ContainsKey(this.query_original[i])) { query_original[i] = MostSimilarWord(query_original[i], Dic_Ref); }
        }
        string sugerencia = "";
        for (int i = 0; i < this.query_original.Length; i++)
        {
            sugerencia += query_original[i] + " ";
        }
        return sugerencia;
    }
}