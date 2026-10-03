namespace Conde.Sufixo;

/// <summary>
/// A funcao Z: para cada posicao i, o tamanho do maior prefixo do texto que
/// tambem comeca em i.
///
/// E a estrutura de sufixo mais barata que existe: vinte linhas, tempo linear,
/// sem alfabeto, sem ordenacao e sem memoria alem do proprio vetor. Com ela se
/// resolve busca de padrao, periodo de cadeia e bordas, que sao tres quartos do
/// que se pede de processamento de texto.
///
/// O algoritmo mantem a JANELA [e, d) mais a direita que ja se sabe ser igual a
/// um prefixo do texto. Para uma posicao nova dentro dessa janela, o valor ja
/// foi calculado numa posicao anterior, e so e preciso continuar comparando
/// quando ele encosta na borda. E esse reaproveitamento que da o tempo linear:
/// o ponteiro direito so anda para a frente, nunca volta.
///
/// O erro classico e usar o valor reaproveitado sem limita-lo ao que sobra da
/// janela. Isso da respostas maiores que a verdade em cadeias periodicas, e so
/// nelas, que e exatamente o caso que um teste com texto sorteado nunca gera.
/// </summary>
public static class Zeta
{
    /// <summary>
    /// O vetor Z. Por convencao Z[0] vale o tamanho do texto, porque o prefixo
    /// inteiro comeca na posicao zero.
    /// </summary>
    public static int[] Calcular(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var n = texto.Length;
        var z = new int[n];
        if (n == 0) return z;
        z[0] = n;

        int e = 0, d = 0;
        for (var i = 1; i < n; i++)
        {
            // Dentro da janela: reaproveita o valor da posicao espelhada, mas
            // NUNCA alem do que sobra da janela. Sem esse limite, cadeias
            // periodicas dao respostas maiores que a verdade.
            if (i < d) z[i] = Math.Min(d - i, z[i - e]);
            while (i + z[i] < n && texto[z[i]] == texto[i + z[i]]) z[i]++;
            if (i + z[i] > d) { e = i; d = i + z[i]; }
        }
        return z;
    }

    /// <summary>O vetor Z pela definicao, caractere por caractere.</summary>
    public static int[] PorForcaBruta(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var n = texto.Length;
        var z = new int[n];
        if (n == 0) return z;
        z[0] = n;
        for (var i = 1; i < n; i++)
            while (i + z[i] < n && texto[z[i]] == texto[i + z[i]]) z[i]++;
        return z;
    }

    /// <summary>
    /// Todas as ocorrencias de um padrao, pelo truque da CONCATENACAO.
    ///
    /// Monta-se padrao + separador + texto e calcula-se o Z. Onde o valor
    /// alcanca o tamanho do padrao, ha uma ocorrencia. O separador precisa ser
    /// um caractere que nao aparece em nenhum dos dois, senao uma ocorrencia
    /// pode atravessar a emenda e o resultado passa a ter posicoes que nao
    /// existem.
    /// </summary>
    public static List<int> Ocorrencias(string texto, string padrao, char separador = '')
    {
        ArgumentNullException.ThrowIfNull(texto);
        ArgumentNullException.ThrowIfNull(padrao);
        var saida = new List<int>();
        if (padrao.Length == 0 || padrao.Length > texto.Length) return saida;
        if (texto.Contains(separador) || padrao.Contains(separador))
            throw new ArgumentException("o separador aparece no texto ou no padrao", nameof(separador));

        var junto = padrao + separador + texto;
        var z = Calcular(junto);
        for (var i = padrao.Length + 1; i < junto.Length; i++)
            if (z[i] >= padrao.Length) saida.Add(i - padrao.Length - 1);
        return saida;
    }

    /// <summary>
    /// O menor PERIODO do texto: o menor p tal que o texto e uma repeticao de um
    /// pedaco de tamanho p, possivelmente cortada no fim.
    ///
    /// A conta sai do Z direto: p e a menor posicao i com i + Z[i] igual ao
    /// tamanho do texto. Se nao houver nenhuma, o periodo e o texto inteiro.
    ///
    /// Isso responde de graca uma pergunta que aparece o tempo todo: "esta
    /// cadeia e uma repeticao de alguma coisa?". Basta o periodo dividir o
    /// tamanho.
    /// </summary>
    public static int MenorPeriodo(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        if (texto.Length == 0) return 0;
        var z = Calcular(texto);
        for (var i = 1; i < texto.Length; i++)
            if (i + z[i] == texto.Length) return i;
        return texto.Length;
    }

    /// <summary>
    /// A funcao de prefixo de Knuth, Morris e Pratt: para cada posicao, o maior
    /// prefixo proprio que tambem e sufixo do pedaco ate ali.
    ///
    /// Ela carrega a mesma informacao que o Z, por outro caminho, e as duas se
    /// convertem uma na outra. Esta esta aqui porque ela e a que da as BORDAS
    /// de forma direta, e porque comparar as duas e o melhor teste que existe
    /// para as duas: sao construcoes sem nada em comum que precisam concordar.
    /// </summary>
    public static int[] Prefixo(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var n = texto.Length;
        var pi = new int[n];
        for (var i = 1; i < n; i++)
        {
            var k = pi[i - 1];
            while (k > 0 && texto[i] != texto[k]) k = pi[k - 1];
            if (texto[i] == texto[k]) k++;
            pi[i] = k;
        }
        return pi;
    }

    /// <summary>
    /// Todas as BORDAS do texto: os prefixos que tambem sao sufixos, do maior
    /// para o menor.
    ///
    /// Elas saem seguindo a funcao de prefixo a partir do fim, e a cadeia tem no
    /// maximo log n passos distintos em tamanho. E a estrutura por tras de quase
    /// todo algoritmo de casamento de padrao.
    /// </summary>
    public static List<int> Bordas(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var saida = new List<int>();
        if (texto.Length == 0) return saida;
        var pi = Prefixo(texto);
        for (var k = pi[^1]; k > 0; k = pi[k - 1]) saida.Add(k);
        return saida;
    }
}
