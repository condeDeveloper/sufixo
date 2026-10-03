# sufixo

Estruturas de sufixo do zero em C# e .NET 8: vetor de sufixos por dobra, Kasai,
função Z, função de prefixo, autômato de sufixos, transformada de Burrows e
Wheeler e Manacher. O juiz ordena as cadeias uma comparação de caractere por
vez, e a comparação é em **todas** as cadeias de até doze caracteres.

```
$ dotnet medidor.dll automato

== o automato de sufixos, com 100,000 caracteres ==
texto                        estados   limite 2n-1    transicoes    clones        subcadeias
todo igual                   100,001       199,999       100,000         0           100,000
dois simbolos                199,969       199,999       275,392    99,968     4,998,499,470
quatro simbolos              162,118       199,999       254,200    62,117     4,999,299,986
vinte e seis                 127,570       199,999       227,155    27,569     4,999,757,400
periodico de 10              100,001       199,999       100,009         0           999,955
a mais b...b                 199,999       199,999       199,999    99,998           199,999
```

**Cinco bilhões de subcadeias distintas em duzentos mil estados.** E a última
linha encosta exatamente no limite provado de 2n-1.

## Construções indiretas precisam de um juiz direto

Quase tudo aqui é construção indireta. O vetor de sufixos não ordena sufixos:
ele ordena por prefixos de tamanho 2^k e dobra a cada passo, e o resultado só é
o vetor de sufixos se uma cadeia de igualdades de classe estiver certa. A função
Z não compara caracteres do zero: ela reaproveita o espelho de uma janela. O
autômato não guarda subcadeias: ele guarda classes de posições finais.

Um erro em qualquer uma delas produz uma resposta **plausível**: uma permutação
quase ordenada, um vetor de valores quase certos, um autômato que reconhece
quase as subcadeias certas. Nenhuma delas falha de forma visível.

Então o juiz aqui é direto e burro: ordena as n cadeias comparando caractere por
caractere, conta os prefixos comuns um a um, monta o conjunto de todas as
subcadeias. Custa O(n²) ou pior, e é exatamente por isso que serve.

E a comparação não é por amostra. São **todas** as cadeias de até 12 caracteres
sobre dois símbolos — 8.190 textos — e todas as de até 7 sobre três. Cadeia
sorteada esconde justamente os casos que quebram: texto todo igual, texto
periódico, sufixo que é prefixo de outro.

## O que as medidas me corrigiram

**A dobra ordena rotações, não sufixos.** A construção usa `(i + k) mod n`,
então o fim do texto emenda no começo. Para "aaa" ela dá a ordem 0, 1, 2 quando
o vetor de sufixos de verdade é 2, 1, 0, porque o sufixo mais curto é o menor.
A correção é pendurar uma sentinela menor que tudo no fim. Não era um caso de
borda: a primeira versão errava em **1.986 das 2.046** cadeias de até dez
caracteres sobre dois símbolos.

**A BWT de "banana" não é "annbaa".** É `nnbaaa`. O famoso "annb$aa" é a
transformada de "banana**$**", com a sentinela que o índice FM precisa — com ela
a matriz de rotações é outra, e a resposta também. Eu escrevi o exemplo de
cabeça e o teste contra a matriz corrigiu.

**O alfabeto maior sai mais barato no autômato.** É o contrário do que se
espera. O que infla o autômato é a **clonagem**, e ela acontece quando um mesmo
trecho aparece em contextos diferentes. Com dois símbolos tudo se repete e quase
todo estado precisa ser partido: 99.968 clones. Com vinte e seis, as repetições
somem e sobram 27.569 — e o autômato fica com 127 mil estados em vez de 200 mil,
representando **mais** subcadeias.

**As rodadas de dobra são log log n em texto comum, não log n.** A dobra para
quando todas as classes ficam distintas, e com quatro símbolos isso acontece
assim que 4^(2^k) passa de n. Com cem mil caracteres são **cinco** rodadas, não
as dezessete que o limite sugere. O pior caso existe e é o texto todo igual, onde
as classes só se separam no fim.

**O índice da linha original da BWT não é único.** Quando o texto é periódico,
duas rotações são a **mesma** cadeia e a ordem entre elas é arbitrária: em "aa"
as duas linhas são "aa". Meu teste comparava o índice com o da força bruta e
falhava por isso. O que precisa valer é que o índice devolvido **inverta**, e é
isso que o teste confere agora.

## O Kasai é a observação mais elegante do repositório

O vetor de prefixos comuns entre vizinhos é o que transforma o vetor de sufixos
de uma permutação ordenada numa estrutura que responde coisas. A construção
óbvia compara cada par de vizinhos do zero e custa O(n²).

Kasai, 2001, observou uma linha: se o sufixo que começa em i tem prefixo comum
de tamanho h com o vizinho dele, então o sufixo que começa em i+1 tem prefixo
comum de **ao menos h-1** com o vizinho dele. Percorrendo as posições na ordem
do **texto** e não na do vetor, o contador h só cai de um em um, e o total de
comparações é O(n).

```
$ dotnet medidor.dll kasai

texto                            n    comp. direta   comp. Kasai     razao
todo igual                  20,000     200,009,999        40,000    5000.2x
periodico de 10             20,000     199,830,044        40,000    4995.8x
dois simbolos               20,000         283,394        40,000       7.1x
vinte e seis                20,000          67,876        40,000       1.7x
```

A coluna do Kasai é o **limite provado**, 2n, e não uma medida: h sobe n vezes no
total e cai no máximo uma por posição, então não há o que medir. E repare na
última linha: em texto de alfabeto grande a construção ingênua é mais barata. O
Kasai existe para o caso repetitivo, que é justamente o que texto sorteado não
gera.

## A transformada não comprime nada

Ela reordena. A compressão vem depois, de qualquer método que goste de
repetição:

```
$ dotnet medidor.dll bwt

texto                     blocos antes   blocos depois     reducao
frase repetida                  50,000              26      99.9 %
periodico de 10                 50,000              10     100.0 %
dois simbolos                   24,906          24,892       0.1 %
vinte e seis                    48,116          48,116       0.0 %
```

Em texto com estrutura, cinquenta mil blocos de caracteres repetidos viram
vinte e seis. Em texto sorteado, nada — não há contexto repetido para agrupar, e
dizer que "a BWT comprime" sem essa ressalva é dizer meia verdade.

E ela é **invertível** guardando só um índice, sem a matriz. A inversão se apoia
numa propriedade que parece mágica e é só ordenação estável: a k-ésima
ocorrência de um caractere na última coluna é a **mesma** ocorrência dele na
primeira.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Juiz.cs` | ordena as cadeias caractere por caractere, e gera todas as cadeias de um tamanho |
| `Vetor.cs` | vetor de sufixos por dobra, com sentinela, e o Kasai |
| `Zeta.cs` | função Z, função de prefixo, período e bordas |
| `Automato.cs` | autômato de sufixos, com a clonagem |
| `Bwt.cs` | a transformada, a inversa e a medida de agrupamento |
| `Palindromo.cs` | Manacher, com o truque da intercalação |

## Como rodar

```
dotnet test testes/Sufixo.Testes/Sufixo.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `automato`, `bwt`, `kasai`, `tamanho` e `tudo`.

## Licença

MIT.
