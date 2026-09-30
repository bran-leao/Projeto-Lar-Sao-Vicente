# Scripts do banco de dados

Dois arquivos, gerados a partir das migrations do projeto. Nenhum deles é
necessário para o sistema funcionar — o banco é criado automaticamente na
inicialização da aplicação. Servem para consulta, documentação e demonstração.

| Arquivo | Para quê |
|---------|----------|
| `banco-lar-sao-vicente.sql` | Criação das tabelas. Idempotente: pode ser executado mais de uma vez sem erro |
| `consultas-demonstracao.sql` | Doze consultas comentadas, para mostrar o banco funcionando |

## Como usar

Rode a aplicação ao menos uma vez (`F5` no Visual Studio, ou
`dotnet run --project src/LarSaoVicente.Web`). O banco é criado e os dados de
demonstração são gravados.

Depois, no Visual Studio: **Exibir → Pesquisador de Objetos do SQL Server**,
botão direito no banco `LarSaoVicente` → **Nova Consulta**, e abra um dos
arquivos. Selecione uma consulta por vez e execute com `Ctrl+Shift+E`.

## Sobre as consultas de demonstração

A mais importante é a **número 5**, do estoque. Não existe coluna "estoque" em
lugar nenhum do banco: o estoque *é* aquela consulta — a soma das entradas cuja
situação já foi conferida. A alternativa seria mover o item de uma área de
triagem para o estoque após a revisão, e uma transferência pode falhar pela
metade, duplicar o registro ou ser esquecida.

A consulta **12** está comentada de propósito: ela grava e apaga dados dentro de
uma transação com `ROLLBACK`, para demonstrar ao vivo que o índice único com
filtro aceita vários itens sem código de barras e recusa um código repetido.

Os dados de demonstração são fictícios. Nenhum dado real de residente do Lar São
Vicente de Paulo é utilizado, em nenhuma circunstância.
