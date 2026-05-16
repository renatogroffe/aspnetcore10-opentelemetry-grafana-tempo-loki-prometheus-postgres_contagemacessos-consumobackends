namespace APIContagem.Models;

public class ResultadoContador
{
    public int ValorAtual { get; set; }
    public string? Local { get; set; }
    public string? Kernel { get; set; }
    public string? Framework { get; set; }
    public string? Mensagem { get; set; }
    public ResultadoAPIExterna? ResultadoAPIExternaJava { get; set; }
    public ResultadoAPIExterna? ResultadoAPIExternaNodeJs { get; set; }
}