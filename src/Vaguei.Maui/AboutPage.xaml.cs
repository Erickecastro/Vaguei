namespace Vaguei.Maui;

public partial class AboutPage : ContentPage
{
    private static readonly IReadOnlyDictionary<string, string> Sections =
        new Dictionary<string, string>
        {
            ["Sobre"] =
                "NOSSO PROPÓSITO\n\nO Vaguei ajuda pessoas de diferentes áreas a descobrir, filtrar e organizar oportunidades profissionais. Cada vaga mantém sua fonte e endereço original para candidatura no canal oficial.\n\nCOMO FUNCIONA\n\nA busca consulta APIs e páginas públicas permitidas, normaliza resultados, remove duplicidades e aplica filtros. O currículo é analisado localmente e a compatibilidade é uma estimativa explicável, não uma decisão de contratação.\n\nO Vaguei não é afiliado às empresas ou plataformas exibidas e não garante entrevistas ou contratações.",
            ["Privacidade"] =
                "PROCESSAMENTO LOCAL\n\nO currículo é processado no dispositivo. A versão atual não possui conta, backend, telemetria nem upload do arquivo. Dados de contato desnecessários são descartados antes da análise.\n\nDADOS ARMAZENADOS\n\nTema, preferências de busca, cache de vagas e identificadores de favoritos ficam localmente. O arquivo do currículo não é enviado às fontes.\n\nSITES EXTERNOS\n\nAo abrir uma vaga, você passa a utilizar o site da empresa ou plataforma e deve consultar a política daquele serviço.",
            ["Termos"] =
                "USO DAS VAGAS\n\nConfirme empresa, requisitos, validade e endereço antes de se candidatar. O Vaguei não cobra pela candidatura.\n\nUSO RESPONSÁVEL\n\nNão utilize o aplicativo para candidaturas automatizadas em massa, fraude, violação de controles de acesso ou atividade contrária à lei e aos termos das fontes.\n\nLIMITAÇÕES\n\nFontes externas podem mudar ou interromper serviços. Não existe garantia de cobertura completa ou disponibilidade contínua.",
            ["Licenças"] =
                "VAGUEI — MIT LICENSE\n\nCopyright © 2026 Ericke Castro. O software pode ser usado, copiado, modificado e distribuído nos termos da licença MIT, mantendo o aviso de copyright e a licença.\n\nCONTEÚDO DE TERCEIROS\n\nA licença do código não concede direitos sobre marcas, logotipos, descrições de vagas ou dados fornecidos por terceiros.",
            ["Fontes"] =
                "INTEGRAÇÕES DISPONÍVEIS\n\nArbeitnow, Ashby, Greenhouse, InHire, Jobicy, Lever, Remotive, SmartRecruiters e Workable usam endpoints públicos ou páginas de carreira permitidas. Jooble é ativada com chave oficial.\n\nPARCERIAS\n\nGupy, InfoJobs, Jobbol e Catho dependem de autorização específica. O LinkedIn não oferece API pública para importação livre do catálogo e não é coletado sem autorização.\n\nToda vaga mantém a plataforma de origem e o link oficial."
        };

    public AboutPage()
    {
        InitializeComponent();
        ShowSection("Sobre");
    }

    private void OnSectionClicked(object? sender, EventArgs eventArgs)
    {
        if (sender is Button { CommandParameter: string section })
            ShowSection(section);
    }

    private void ShowSection(string section)
    {
        SectionTitle.Text = section;
        SectionBody.Text = Sections[section];
    }

    private async void OnCloseClicked(object? sender, EventArgs eventArgs) =>
        await Navigation.PopModalAsync();
}
