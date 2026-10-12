namespace PedidosCrud.Model;

public sealed class InputValidationException(Dictionary<string, string[]> errors) : Exception("Verifique os campos informados.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}

public sealed class OrderNotFoundException(int id) : Exception($"O pedido {id} não foi encontrado.");

public sealed class OrderConflictException() : Exception("Este pedido foi alterado por outra requisição. Atualize os dados e tente novamente.");
