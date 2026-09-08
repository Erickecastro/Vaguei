# Preparação do Vaguei MAUI para Produção

## Objetivo

Preparar o cliente Android experimental para chegar ao usuário final com interface
fluida, busca confiável, privacidade preservada e operação sustentável. O aplicativo
continua sendo uma aplicação .NET MAUI: o uso direto de um componente Android fica
restrito à lista de vagas, onde centenas de elementos exigem a reciclagem e a física
de rolagem do `RecyclerView`.

## Decisão de arquitetura

| Área | Implementação recomendada | Motivo |
| --- | --- | --- |
| Aplicação, tema, ciclo de vida, navegação e estados | MAUI puro | Compartilhamento de código e comportamento consistente. |
| Tela inicial, currículo, pesquisa e filtros | MAUI puro | Poucos controles, boa acessibilidade e nenhuma necessidade de virtualização. |
| Sobre, privacidade, termos, licenças e fontes | MAUI puro | Conteúdo estático, simples de manter e adaptável a iOS no futuro. |
| Avisos de conexão, confirmação de saída e diálogos | MAUI puro | Identidade visual própria e controle completo de tema. |
| Seleção de documento | API MAUI (`FilePicker`) | Usa o Storage Access Framework e concede acesso somente ao arquivo escolhido. |
| Lista extensa de vagas no Android | `RecyclerView` nativo encapsulado por handler MAUI | Rolagem, inércia, reciclagem e `ViewHolder` nativos para centenas de cards. |
| Futuro iOS | Interface MAUI equivalente para resultados, com avaliação de `UICollectionView` somente se necessário | Evita antecipar complexidade antes de validar o produto. |

O `AndroidJobListView` não representa uma segunda interface: ele recebe os mesmos
`JobResultItemViewModel`, tema, comandos de favorito e abertura de links do restante
da aplicação MAUI. Não há motivo para reconstruir as demais telas em código nativo
Android neste momento.

## Busca de vagas: prioridade máxima

### Estado atual

- Nove fontes públicas independentes são iniciadas em paralelo.
- Cache persistente de 30 minutos reduz consultas repetidas.
- Leitura de cache, coleta, normalização, deduplicação e matching não ocupam a
  thread visual.
- A busca publica lotes por fonte concluída e exibe a primeira lista útil ao
  alcançar cinco vagas compatíveis ou duas fontes concluídas, enquanto a
  cobertura restante continua.
- O adaptador Android usa chaves estáveis e `DiffUtil` para aplicar a
  consolidação sem recriar a lista, interromper o gesto ou perder a posição.
- Cada fonte possui limite individual, falhas isoladas e prazo geral de busca.
- O aplicativo nunca deve expor detalhes técnicos de falha ao usuário.

### Próxima evolução: feedback discreto da atualização progressiva

A melhoria de maior impacto é separar **primeiros resultados úteis** de
**cobertura completa**:

```text
consulta iniciada
      |
      +-- fontes rápidas / cache -> exibir primeiras vagas utilizáveis
      |
      +-- fontes restantes -> normalizar, deduplicar e atualizar sem tirar o usuário da lista
      |
      +-- prazo final -> manter resultados já obtidos e informar cobertura resumida
```

Os dois primeiros estágios já estão implementados: publicação progressiva e
atualização diferencial da lista nativa. A próxima etapa recomendada é:

1. Manter uma mensagem discreta como “Atualizando mais fontes…” sem bloquear a lista.
2. Ao final, atualizar somente o texto resumido de cobertura; não exibir erros técnicos.

Isso reduz o tempo até a primeira vaga sem abandonar fontes lentas nem diminuir a
qualidade da busca final.

### Cache e rede

- Transformar o cache persistente em API assíncrona para evitar qualquer leitura ou
  gravação de JSON síncrona em caminhos futuros.
- Usar modelo *stale-while-revalidate*: exibir um cache ainda recente imediatamente e
  renovar em segundo plano, deixando claro quando novos resultados chegaram.
- Guardar por fonte métricas locais agregadas de latência, sucesso e quantidade, sem
  registrar currículo, nome, e-mail, telefone ou consulta completa do usuário.
- Definir orçamento por fonte e concorrência adaptativa: fontes historicamente lentas
  não devem atrasar fontes rápidas nem saturar a rede móvel.
- Preferir endpoints oficiais paginados, filtros remotos e campos mínimos; evitar baixar
  catálogos completos quando uma fonte oferece consulta por palavra-chave ou data.
- Revalidar autorização, limites de uso e termos antes de adicionar cada plataforma.

### Qualidade dos resultados

- Persistir a cobertura por fonte e detectar quedas de qualidade de um conector.
- Manter deduplicação entre plataformas e identificadores estáveis.
- Avaliar sinônimos e expansão de busca com exemplos reais, impondo limite para não
  aumentar ruído nem multiplicar requisições sem necessidade.
- Introduzir testes de regressão com consultas representativas: estágio, júnior,
  desenvolvedor, enfermagem, administração, Manaus e Exterior.
- Exibir compatibilidade somente após análise local de currículo, mantendo buscas
  diretas livres de pontuação artificial.

## Desempenho de interface

### Já adotado

- `RecyclerView`, `LinearLayoutManager` e `ViewHolder` para cards de vagas.
- Sem animação de alteração de item durante rolagem.
- Reciclagem, pré-busca moderada e cards de altura adaptativa.
- Matching e coleta fora da thread de interface.
- Lista renderizada somente depois da consolidação atual de resultados.

### Melhorias recomendadas

1. Medir tempo de inicialização, primeira interação, primeira vaga e conclusão de
   busca em aparelhos de entrada, intermediários e topo de linha.
2. Substituir recargas completas da lista por atualizações diferenciais quando forem
   implementados resultados progressivos.
3. Evitar imagens remotas dentro dos cards até existir cache de imagens e orçamento de
   memória; empresa, local, fonte e data já oferecem a informação essencial.
4. Manter títulos truncados em duas linhas e campos em uma linha para limitar custo de
   layout e preservar legibilidade.
5. Executar profiling de CPU, memória, GC, rede e quadros com `adb`, Android Studio
   Profiler e testes de rolagem em listas com 50, 200 e 500 vagas.
6. Testar fontes e tamanhos de exibição do Android; texto ampliado não pode cortar as
   ações de favorito ou candidatura.

## Refinamentos de experiência e UI

- Concluir a introdução, busca, filtros e resultados com transições que nunca atrasem
  a interação ou escondam estados importantes.
- Manter modais centralizados, com barreira de toque, botão de fechar e retorno do
  sistema previsível.
- Dar feedback claro para cache, busca em andamento, falta de rede, zero resultados e
  resultados parciais, sempre em linguagem não técnica.
- Confirmar acessibilidade: rótulos semânticos, contraste, ordem de foco, tamanho de
  toque de pelo menos 44 dp, leitor de tela e navegação por teclado externo.
- Testar tema claro e escuro em todas as telas, inclusive splash, seletor de filtros,
  Sobre, avisos e cards nativos.
- Aplicar internacionalização gradual: português brasileiro primeiro, estrutura pronta
  para inglês e formatação regional de datas.

## Privacidade, segurança e operação

- Manter currículos locais, temporários e processados no dispositivo.
- Solicitar apenas acesso ao documento escolhido; não pedir permissão ampla para fotos,
  vídeos ou armazenamento.
- Não embutir segredos de agregadores no APK. Plataformas que exigem segredo devem usar
  API oficial com restrição por aplicativo ou backend seguro antes do lançamento.
- Criar canal de suporte e fluxo de reporte de vulnerabilidade antes da distribuição.
- Revisar LGPD, termos, política de privacidade, retenção de dados e consentimento antes
  de qualquer telemetria ou conta de usuário.
- Configurar assinatura de release, armazenamento seguro de keystore, CI reprodutível,
  análise de dependências e verificação de segredos antes de Play Store.

## Critérios de saída para produção

- Busca direta e por currículo confiáveis em rede móvel e Wi-Fi.
- Primeira vaga exibida rapidamente, cobertura completa concluída sem bloquear a UI.
- Rolagem contínua em listas extensas, sem cards cortados ou toques acidentais.
- Nenhuma credencial no repositório ou APK.
- Testes automatizados, testes manuais por dispositivo e regressões de busca aprovados.
- Política de privacidade, termos, fontes autorizadas e suporte ao usuário publicados.
- APK/AAB de release assinado, versionado e instalado por canal de testes fechado antes
  da publicação ampla.

## Ordem recomendada de execução

1. Resultados progressivos com atualização diferencial da lista.
2. Cache assíncrono e estratégia *stale-while-revalidate*.
3. Medição de latência e cobertura por fonte, sem dados pessoais.
4. Bateria de testes em dispositivos e redes reais.
5. Acessibilidade, internacionalização e textos finais de UX.
6. Integrações autorizadas, segurança de credenciais e backend somente se necessário.
7. Assinatura, distribuição fechada e preparação para Play Store.
