# Vaguei 

O **Vaguei** é um projeto pessoal que estou desenvolvendo para facilitar uma das partes mais cansativas da busca por emprego na área de tecnologia: encontrar vagas que realmente façam sentido para o meu perfil.

A ideia surgiu da vontade de criar uma ferramenta capaz de analisar meu currículo, entender minhas tecnologias, experiências e áreas de interesse e, a partir dessas informações, encontrar e organizar vagas de desenvolvimento de software que tenham maior compatibilidade com o meu perfil.

Além de ser uma ferramenta que pretendo utilizar no meu próprio dia a dia, o Vaguei também é um projeto de estudo onde posso aplicar e aprofundar meus conhecimentos em **C#, .NET, arquitetura de software, processamento de dados, APIs e automação**.

## 💡 Como vai funcionar

O objetivo é permitir que um currículo seja fornecido ao Vaguei e transformado em um perfil estruturado.

Atualmente, o projeto já consegue:

* Ler currículos no formato `.odt`;
* Extrair o conteúdo do documento;
* Identificar informações básicas do candidato;
* Identificar linguagens, frameworks e outras tecnologias;
* Reconhecer aliases de tecnologias;
* Extrair experiências profissionais;
* Identificar empresas, cargos e períodos das experiências.

A ideia é evoluir esse processo para também pesquisar vagas em diferentes fontes e comparar os requisitos encontrados com o perfil extraído do currículo.

## 🎯 Objetivo

O fluxo principal que pretendo construir é:

```text
Currículo
   ↓
Extração de texto
   ↓
Análise do perfil
   ↓
CandidateProfile
   ↓
Busca de vagas
   ↓
Análise das vagas
   ↓
Matching
   ↓
Vagas mais compatíveis
```

Cada vaga poderá receber uma pontuação de compatibilidade considerando fatores como tecnologias, experiência, senioridade, localização e modelo de trabalho.

No futuro, também pretendo utilizar IA como uma camada complementar para ajudar na interpretação das vagas e explicar os motivos pelos quais uma oportunidade pode ou não combinar com o perfil.

## 🛠️ Stack

O projeto está sendo desenvolvido principalmente com:

* **C#**
* **.NET 10**
* **xUnit**
* **XML / OpenDocument**
* **Git e GitHub**

A arquitetura foi organizada para manter as responsabilidades separadas entre domínio, regras da aplicação, leitura de currículos, coleta de vagas e infraestrutura.

```text
Vaguei
├── Vaguei.Domain
├── Vaguei.Application
├── Vaguei.ResumeParser
├── Vaguei.Collectors
├── Vaguei.Infrastructure
├── Vaguei.Cli
└── Vaguei.Tests
```

## 📄 Formatos de currículo

O primeiro formato implementado foi o **OpenDocument Text (`.odt`)**.

A ideia é adicionar suporte também para:

* DOCX
* PDF
* TXT

Independentemente do formato, todos os documentos serão convertidos para uma representação comum antes da análise.

## 🚧 Status

O Vaguei ainda está em desenvolvimento e muitas funcionalidades serão adicionadas e modificadas durante a evolução do projeto.

Neste momento, o foco está na construção da base responsável por interpretar o currículo e gerar um perfil estruturado. As próximas etapas incluem suporte a novos formatos de documento, coleta de vagas, sistema de matching e uma interface gráfica.

---

Feito como projeto pessoal para aprender, experimentar e, quem sabe, tornar a procura pela próxima vaga um pouco mais inteligente. 🚀
