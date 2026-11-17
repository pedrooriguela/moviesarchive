# Recomendações personalizadas

A tela inicial mostra, antes de "Mais bem avaliados", a prateleira **"Recomendado para você"**,
com até 10 filmes escolhidos a partir do que a pessoa já fez no site. Cada cartão diz por que o
filme foi recomendado (por exemplo, "Porque você gostou de Psicose").

O método é **recomendação por semelhança de conteúdo**: o site aprende de que gêneros e diretores a
pessoa gosta e procura outros filmes com esses gêneros e diretores. Ele funciona mesmo com uma única
pessoa usando o site, ao contrário da recomendação colaborativa ("quem assistiu X também assistiu
Y"), que precisa de muitos usuários com históricos parecidos.

## Onde está no código

| O quê | Onde |
|---|---|
| O cálculo (pesos, perfil, pontuação, motivo) | `MoviesArchive.Domain/Models/Recommendations/RecommendationEngine.cs` |
| Os dados de entrada | `ViewerSignals.cs` (mesma pasta) e `MovieRepository.GetViewerSignalsAsync` |
| A prateleira na tela inicial | `MovieCatalogService.GetHomeAsync` (rota `GET /api/movies/home`) |
| O cartão com o motivo e a marca "Visto" | `MoviesArchive.Client/src/components/MovieCard.jsx` |

O `RecommendationEngine` não acessa o banco: recebe o catálogo, os sinais da pessoa e as médias da
comunidade, e só calcula. Todos os pesos estão em constantes no topo do arquivo.

## Passo 1: o peso de cada filme com que a pessoa interagiu

Três tabelas dizem algo sobre o gosto da pessoa. Cada filme recebe a soma dos pesos que se aplicam:

| Sinal | De onde vem | Peso |
|---|---|---|
| Assistiu | `WatchHistory` (apertou o play) | **+1** |
| Assistiu de novo | cada play além do primeiro | **+0,25**, no máximo 2 vezes (+0,5) |
| Gostou | `Reviews`, nota de 4 para cima | **+2** |
| Não gostou | `Reviews`, nota de 2 para baixo | **−1** |
| Quer ver | `Watchlist`, se ainda não assistiu | **+0,5** |

Notas entre 2,5 e 3,5 não somam nem tiram. Assistir e não gostar dá 1 − 1 = **0**: o filme deixa de
influenciar o perfil.

## Passo 2: o perfil (gêneros e diretores)

Para cada gênero, soma-se o peso de todos os filmes que têm esse gênero. O mesmo para cada diretor.
Depois, cada soma é dividida pela maior delas:

```
afinidade(gênero) = soma dos pesos dos filmes com o gênero / maior soma entre os gêneros
afinidade(diretor) = soma dos pesos dos filmes do diretor / maior soma entre os diretores
```

Assim o gênero preferido vale **1** e os demais, uma fração. Valores negativos (gêneros de filmes de
que a pessoa não gostou) ficam limitados a **−1**.

## Passo 3: os candidatos

Entram todos os filmes que a pessoa **ainda não assistiu, não avaliou e não guardou na watchlist**.

O filtro de classificação indicativa já vale nessa consulta: um menor de idade só recebe
recomendações de filmes da idade dele.

## Passo 4: a pontuação de cada candidato

```
pontuação = gêneros + diretor + comunidade

gêneros    = média da afinidade dos gêneros do filme (gênero fora do perfil conta 0)
diretor    = 0,5 × afinidade do diretor (só quando a afinidade é positiva)
comunidade = 0,2 × (média bayesiana − 3) / 2

média bayesiana = (3 × 3 + soma das notas) / (3 + número de avaliações)
```

- **Gêneros** é o critério principal. Usar a média (e não a soma) evita favorecer filmes só por terem
  muitos gêneros.
- **Diretor** é um bônus: vale metade de um gênero. Diretor de que a pessoa não gostou não tira pontos.
- **Comunidade** só desempata. A média bayesiana "finge" que todo filme já tem 3 notas 3, então uma
  única nota 5 pesa pouco. O resultado fica entre −0,25 e +0,2, e filme sem avaliações fica neutro (0).
- Filme sem nada em comum com o perfil (gêneros + diretor ≤ 0) **não entra**, mesmo que a comunidade
  goste muito dele.

Os 10 de maior pontuação formam a prateleira. Empates são resolvidos pela nota da comunidade e
depois pelo título.

## Passo 5: o motivo mostrado no cartão

Para cada recomendado, procura-se o filme do histórico que mais contribuiu:

```
contribuição = peso do filme do histórico × semelhança

semelhança = fração dos gêneros do recomendado que o filme do histórico também tem
           + 0,5 se for do mesmo diretor
```

O texto depende do sinal mais forte desse filme:

| Sinal do filme que mais contribuiu | Texto |
|---|---|
| Nota de 4 para cima | "Porque você gostou de *título*" |
| Assistiu | "Porque você assistiu *título*" |
| Só está na watchlist | "Parecido com *título*, da sua watchlist" |

## Quando a prateleira não aparece

- A pessoa não está logada.
- A pessoa ainda não assistiu, avaliou nem guardou nada.
- Tudo o que ela fez foi negativo (só filmes de que não gostou).
- Nenhum filme ainda não visto tem algo em comum com o perfil.

Nesses casos, a tela inicial fica como antes, sem a prateleira.

## Exemplo calculado

Uma pessoa:

- assistiu **Psicose** duas vezes e deu nota 4,5;
- assistiu **O Iluminado** uma vez e deu nota 2;
- guardou **Casablanca** na watchlist.

Além disso, duas outras pessoas avaliaram **Nosferatu**, com média 4,5.

**Passo 1, pesos**

| Filme | Conta | Peso |
|---|---|---|
| Psicose | 1 (assistiu) + 0,25 (assistiu de novo) + 2 (gostou) | **3,25** |
| O Iluminado | 1 (assistiu) − 1 (não gostou) | **0** (não influencia) |
| Casablanca | 0,5 (watchlist) | **0,5** |

**Passo 2, perfil**

| Gênero | Soma | Afinidade |
|---|---|---|
| Suspense | 3,25 (Psicose) | 3,25 / 3,25 = **1** |
| Terror | 3,25 (Psicose) | **1** |
| Drama | 0,5 (Casablanca) | 0,5 / 3,25 = **0,154** |
| Romance | 0,5 (Casablanca) | **0,154** |

Diretores: Alfred Hitchcock **1**, Michael Curtiz **0,154**.

Nosferatu, na comunidade: média bayesiana = (9 + 4,5 × 2) / (3 + 2) = 3,6, então
comunidade = 0,2 × (3,6 − 3) / 2 = **+0,06**.

**Passos 4 e 5, resultado** (os mesmos números que o código produz)

| # | Filme | Gêneros | Diretor | Comunidade | Pontuação | Motivo |
|---|---|---|---|---|---|---|
| 1 | Um Corpo que Cai (Romance, Suspense; Hitchcock) | (0,154 + 1) / 2 = 0,577 | 0,5 × 1 = 0,5 | 0 | **1,077** | Porque você gostou de Psicose |
| 2 | Nosferatu (Terror) | 1 | 0 | +0,06 | **1,060** | Porque você gostou de Psicose |
| 3 | A Noite dos Mortos-Vivos (Terror) | 1 | 0 | 0 | **1,000** | Porque você gostou de Psicose |
| 4 | O Gabinete do Dr. Caligari (Suspense, Terror) | 1 | 0 | 0 | **1,000** | Porque você gostou de Psicose |
| 5 | Parasita (Drama, Suspense) | (0,154 + 1) / 2 = 0,577 | 0 | 0 | **0,577** | Porque você gostou de Psicose |
| 6 | Oldboy (Ação, Suspense) | (0 + 1) / 2 = 0,5 | 0 | 0 | **0,500** | Porque você gostou de Psicose |
| 7 a 10 | Central do Brasil, Cidadão Kane, Cinema Paradiso, Viagem a Tóquio (dramas) | 0,154 | 0 | 0 | **0,154** | Parecido com Casablanca, da sua watchlist |

Repare que:

- **Um Corpo que Cai** ficou em primeiro por causa do diretor, mesmo tendo só um dos gêneros favoritos.
- **Nosferatu** passou à frente dos outros terrores pela nota da comunidade.
- **O Iluminado** não aparece (já foi visto) e também não puxou o perfil (peso 0).
- Os dramas entraram graças à watchlist, com peso bem menor que o de Psicose.

## Filmes vistos

O mesmo histórico define a marca **"Visto"**: um filme é considerado visto quando a pessoa apertou o
play pelo menos uma vez (tem entrada em `WatchHistory`). A marca aparece no pôster dos cartões (busca,
prateleiras e watchlist) e na ficha do filme ("Você já assistiu").

## Como ajustar

Todos os números estão em constantes no topo do `RecommendationEngine.cs`. Por exemplo:

- dar mais importância ao diretor: aumentar `DirectorFactor`;
- deixar a nota da comunidade pesar mais: aumentar `CommunityFactor`;
- fazer a watchlist influenciar mais: aumentar `WatchlistWeight`.

Se mudar algum valor, atualize também esta página.

## Limites conhecidos

- O perfil usa só gêneros e diretor, porque são os metadados que o acervo tem. Elenco, década e
  idioma poderiam entrar no futuro.
- O cálculo carrega o catálogo inteiro a cada abertura da tela inicial. Com o acervo atual isso é
  instantâneo. Com dezenas de milhares de filmes, valeria pré-calcular ou guardar em cache.
