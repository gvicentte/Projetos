using System;
using System.Data.Common; // For database operations
using System.IO.Compression; // For compression operations
using System.Runtime.InteropServices; // For database operations
using System.Threading.Tasks; // For asynchronous programming
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using System.Data; // Npgsql is the .NET data provider for PostgreSQL

//Complexidade de tempo e espaço para as operações principais:
//Considerando que N é o número de produtos e M o número de itens do pedido:
// - Listar Produtos O(N)
// - Listar Itens do Pedido O(M)
// - Cadastrar Produto O(1)
// - Cadastrar Pedido + itens O(M)
// - Atualizar ou Excluir Produto O(1)
// - Atualizar ou Excluir Pedido O(1)

//Complexidade de espaço:
// - O(N) para armazenar produtos em memória
// - O(M) para armazenar itens do pedido em memória

class Program
{
    const string connectionString = "Host=localhost;Username=postgres;Password=1508;Database=postgres"; //conectou ao banco dessa vez rodando pelo VsCode
    static async Task Main()
    {
        await using var connection = new NpgsqlConnection(connectionString);
        int opcoes = 0;
        Console.WriteLine();
        Console.WriteLine("Ola Seja Bem Vindo ao Sistema de Estoque, Produtos e Pedidos");
        while (opcoes != 3)
        {
            Console.WriteLine("Selecione uma das opcoes abaixo:");
            Console.WriteLine("1 - Sistema de Produtos");
            Console.WriteLine("2 - Sistema de Pedidos");
            Console.WriteLine("3 - Sair do Sistema");
            opcoes = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine();
            switch (opcoes)
            {
                case 1:
                    Console.WriteLine("Seja Bem Vindo ao Sistema de Produtos\n");
                    int opcaoProduto = 0;
                    while (opcaoProduto != 6)
                    {
                        Console.WriteLine("Escolha uma das opcoes abaixo:");
                        Console.WriteLine("1 - Cadastrar Produto");
                        Console.WriteLine("2 - Listar Produtos");
                        Console.WriteLine("3 - Obter Produto por ID");
                        Console.WriteLine("4 - Atualizar Produto");
                        Console.WriteLine("5 - Excluir Produto");
                        Console.WriteLine("6 - Voltar ao Menu Principal");
                        opcaoProduto = Convert.ToInt32(Console.ReadLine());
                        switch (opcaoProduto)
                        {
                            case 1:
                                Console.WriteLine("Cadastrar Produto selecionado.\n");
                                await connection.OpenAsync();
                                if (connection.State == System.Data.ConnectionState.Open)
                                {
                                    Console.Write("Informe o nome do produto: ");
                                    string nome = Console.ReadLine() ?? string.Empty;
                                    Console.Write("Informe a descrição do produto: ");
                                    string? descricao = Console.ReadLine();
                                    Console.Write("Informe o preço do produto: ");
                                    decimal preco = Convert.ToDecimal(Console.ReadLine());
                                    Console.Write("Informe a quantidade em estoque: ");
                                    int quantidadeEstoque = Convert.ToInt32(Console.ReadLine());
                                    using (var cmd = new NpgsqlCommand("INSERT INTO produtos (nome, descricao, preco, quantidade_estoque) VALUES (@nome, @descricao, @preco, @quantidadeEstoque)", connection))
                                    {
                                        cmd.Parameters.AddWithValue("nome", nome);
                                        cmd.Parameters.AddWithValue("descricao", descricao);
                                        cmd.Parameters.AddWithValue("preco", preco);
                                        cmd.Parameters.AddWithValue("quantidadeEstoque", quantidadeEstoque);
                                        if (string.IsNullOrEmpty(nome) || preco <= 0 || quantidadeEstoque < 0)
                                        {
                                            Console.WriteLine("Dados inválidos. Por favor, tente novamente.\n");
                                            return;
                                        }
                                        int linhasAfetadas = await cmd.ExecuteNonQueryAsync();
                                        if (linhasAfetadas > 0)
                                        {
                                            Console.WriteLine("Produto cadastrado com sucesso!\n");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Erro ao cadastrar o produto. Tente novamente.\n");
                                        }
                                    }
                                    connection.Close();
                                }
                                else
                                {
                                    Console.WriteLine("Falha ao conectar ao banco de dados. Tente novamente.");
                                    connection.Close();
                                    return;
                                }
                                break;
                            case 2:
                                Console.WriteLine("Listar Produtos selecionado.\n");
                                await connection.OpenAsync();
                                if (connection.State == System.Data.ConnectionState.Open)
                                {
                                    using (var cmd = new NpgsqlCommand("SELECT * FROM produtos", connection))
                                    {
                                        await using var reader = await cmd.ExecuteReaderAsync();
                                        if (reader.HasRows)
                                        {
                                            Console.WriteLine("Lista de Produtos:");
                                            while (await reader.ReadAsync())
                                            {
                                                //Console.WriteLine($"ID: {reader.GetInt32(0)}, Nome: {reader.GetString(1)}, Descrição: {reader.GetString(2)}, Preço: {reader.GetDecimal(3)}, Quantidade em Estoque: {reader.GetInt32(4)}");
                                                //O de cima e para exibir no console normalmente, sem o formato JSON
                                                var teste = new Produto
                                                {
                                                    Id = reader.GetInt32(0),
                                                    Nome = reader.GetString(1),
                                                    Descricao = reader.IsDBNull(2) ? null : reader.GetString(2),
                                                    Preco = reader.GetDecimal(3),
                                                    QuantidadeEstoque = reader.GetInt32(4)
                                                };
                                                string json = JsonSerializer.Serialize(teste, new JsonSerializerOptions { WriteIndented = true });
                                                Console.WriteLine(json);
                                                Console.WriteLine(); // Adiciona uma linha em branco para melhor legibilidade
                                                // Exibe os detalhes do item do pedido
                                            }
                                            Console.WriteLine("\n");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Nenhum produto encontrado.\n");
                                        }
                                    }
                                    connection.Close();
                                }
                                else
                                {
                                    Console.WriteLine("Falha ao conectar ao banco de dados. Tente novamente.\n");
                                    connection.Close();
                                    return;
                                }
                                break;
                            case 3:
                                Console.WriteLine("Obter Produto por ID selecionado.\n");
                                Console.Write("Informe o ID do produto: ");
                                int idProduto = Convert.ToInt32(Console.ReadLine());
                                await connection.OpenAsync();
                                if (connection.State == System.Data.ConnectionState.Open)
                                {
                                    using (var cmd = new NpgsqlCommand("SELECT * FROM produtos WHERE id = @id", connection))
                                    {
                                        cmd.Parameters.AddWithValue("id", idProduto);
                                        await using var reader = await cmd.ExecuteReaderAsync();
                                        if (await reader.ReadAsync())
                                        {
                                            var produto = new Produto
                                            {
                                                Id = reader.GetInt32(0),
                                                Nome = reader.GetString(1),
                                                Descricao = reader.IsDBNull(2) ? null : reader.GetString(2),
                                                Preco = reader.GetDecimal(3),
                                                QuantidadeEstoque = reader.GetInt32(4)
                                            };
                                            string json = JsonSerializer.Serialize(produto, new JsonSerializerOptions { WriteIndented = true });
                                            Console.WriteLine("Produto encontrado:");
                                            Console.WriteLine(json);
                                            Console.WriteLine(); // Adiciona uma linha em branco para melhor legibilidade
                                        }
                                        else
                                        {
                                            Console.WriteLine("Produto não encontrado.\n");
                                        }
                                    }
                                    connection.Close();
                                }
                                else
                                {
                                    Console.WriteLine("Falha ao conectar ao banco de dados. Tente novamente.\n");
                                    connection.Close();
                                    return;
                                }
                                break;
                            case 4:
                                Console.WriteLine("Atualizar Produto selecionado.\n");
                                Console.Write("Informe o ID do produto a ser atualizado: ");
                                int idAtualizar = Convert.ToInt32(Console.ReadLine());
                                await connection.OpenAsync();
                                if (connection.State == System.Data.ConnectionState.Open)
                                {
                                    using (var cmd = new NpgsqlCommand("SELECT * FROM produtos WHERE id = @id", connection))
                                    {
                                        cmd.Parameters.AddWithValue("id", idAtualizar);
                                        await using var reader = await cmd.ExecuteReaderAsync();
                                        if (await reader.ReadAsync())
                                        {
                                            Console.WriteLine($"ID: {reader.GetInt32(0)}, Nome: {reader.GetString(1)}, Descrição: {reader.GetString(2)}, Preço: {reader.GetDecimal(3)}, Quantidade em Estoque: {reader.GetInt32(4)}");
                                            reader.Close(); // Fecha o leitor antes de atualizar
                                            Console.Write("Informe o novo nome do produto: ");
                                            string novoNome = Console.ReadLine() ?? string.Empty;
                                            Console.Write("Informe a nova descrição do produto: ");
                                            string? novaDescricao = Console.ReadLine();
                                            Console.Write("Informe o novo preço do produto: ");
                                            decimal novoPreco = Convert.ToDecimal(Console.ReadLine());
                                            Console.Write("Informe a nova quantidade em estoque: ");
                                            int novaQuantidadeEstoque = Convert.ToInt32(Console.ReadLine());
                                            using (var updateCmd = new NpgsqlCommand("UPDATE produtos SET nome = @nome, descricao = @descricao, preco = @preco, quantidade_estoque = @quantidadeEstoque WHERE id = @id", connection))
                                            {
                                                updateCmd.Parameters.AddWithValue("nome", novoNome);
                                                updateCmd.Parameters.AddWithValue("descricao", novaDescricao);
                                                updateCmd.Parameters.AddWithValue("preco", novoPreco);
                                                updateCmd.Parameters.AddWithValue("quantidadeEstoque", novaQuantidadeEstoque);
                                                updateCmd.Parameters.AddWithValue("id", idAtualizar);
                                                await updateCmd.ExecuteNonQueryAsync();
                                                Console.WriteLine("Produto atualizado com sucesso!\n");
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("Produto não encontrado.\n");
                                        }
                                    }
                                    connection.Close();
                                }
                                else
                                {
                                    Console.WriteLine("Falha ao conectar ao banco de dados. Tente novamente.\n");
                                    connection.Close();
                                    return;
                                }
                                break;
                            case 5:
                                Console.WriteLine("Excluir Produto selecionado.\n");
                                Console.Write("Informe o ID do produto a ser excluído: ");
                                int idExcluir = Convert.ToInt32(Console.ReadLine());
                                await connection.OpenAsync();
                                if (connection.State == System.Data.ConnectionState.Open)
                                {
                                    using (var cmd = new NpgsqlCommand("DELETE FROM produtos WHERE id = @id", connection))
                                    {
                                        cmd.Parameters.AddWithValue("id", idExcluir);
                                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                                        if (rowsAffected > 0)
                                        {
                                            Console.WriteLine("Produto excluído com sucesso!\n");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Produto não encontrado ou já excluído.\n");
                                        }
                                    }
                                    connection.Close();
                                }
                                else
                                {
                                    Console.WriteLine("Falha ao conectar ao banco de dados. Tente novamente.\n");
                                    connection.Close();
                                    return;
                                }
                                break;
                            case 6:
                                Console.WriteLine("Voltando ao Menu Principal...\n");
                                break;
                            default:
                                Console.WriteLine("Opcao invalida, tente novamente.\n");
                                break;
                        }
                    }
                    break;
                case 2:
                    Console.WriteLine("Seja Bem Vindo ao Sistema de Pedidos\n");
                    int opcaoPedido = 0;
                    while (opcaoPedido != 5)
                    {
                        Console.WriteLine("Escolha uma das opcoes abaixo:");
                        Console.WriteLine("1 - Cadastrar Pedido");
                        Console.WriteLine("2 - Listar Pedido Especifico");
                        Console.WriteLine("3 - Atualizar Pedido");
                        Console.WriteLine("4 - Excluir Pedido");
                        Console.WriteLine("5 - Voltar ao Menu Principal");
                        opcaoPedido = Convert.ToInt32(Console.ReadLine());
                        switch (opcaoPedido)
                        {
                            case 1:
                                Console.WriteLine("Cadastrar Pedido selecionado.\n");
                                await connection.OpenAsync();
                                if (connection.State == System.Data.ConnectionState.Open)
                                {
                                    Console.Write("Nome do cliente: ");
                                    string cliente = Console.ReadLine();
                                    Console.Write("Quantos itens o pedido terá? ");
                                    int totalItens = int.Parse(Console.ReadLine());
                                    List<ItemPedido> itens = new();
                                    decimal valorTotalPedido = 0;
                                    for (int i = 0; i < totalItens; i++)
                                    {
                                        Console.WriteLine($"\nItem {i + 1}:");
                                        Console.Write("ID do Produto: ");
                                        int produtoId = int.Parse(Console.ReadLine());
                                        Console.Write("Quantidade: ");
                                        int quantidade = int.Parse(Console.ReadLine());
                                        // Buscar produto no banco
                                        await using var cmdProduto = new NpgsqlCommand("SELECT nome, preco FROM produtos WHERE id = @id", connection);
                                        cmdProduto.Parameters.AddWithValue("id", produtoId);
                                        await using var reader = await cmdProduto.ExecuteReaderAsync();
                                        if (!await reader.ReadAsync())
                                        {
                                            Console.WriteLine("Produto não encontrado!");
                                            continue;
                                        }
                                        string nomeProduto = reader.GetString(0);
                                        decimal precoUnitario = reader.GetDecimal(1);
                                        decimal valorTotalItem = precoUnitario * quantidade;
                                        valorTotalPedido += valorTotalItem;
                                        itens.Add(new ItemPedido
                                        {
                                            ProdutoId = produtoId,
                                            NomeProduto = nomeProduto,
                                            PrecoUnitario = precoUnitario,
                                            Quantidade = quantidade,
                                            ValorTotalItem = valorTotalItem
                                        });
                                    }
                                    // Inserir pedido
                                    await using var cmdPedido = new NpgsqlCommand("INSERT INTO pedidos (cliente, data_pedido, valor_total) VALUES (@cliente, NOW(), @total) RETURNING id", connection);
                                    cmdPedido.Parameters.AddWithValue("cliente", cliente);
                                    cmdPedido.Parameters.AddWithValue("total", valorTotalPedido);
                                    int pedidoId = (int)await cmdPedido.ExecuteScalarAsync();
                                    // Inserir itens
                                    foreach (var item in itens)
                                    {
                                        await using var cmdItem = new NpgsqlCommand("INSERT INTO itens_pedido (pedido_id, produto_id, nome_produto, preco_unitario, quantidade, valor_total_item) VALUES (@pedidoId, @produtoId, @nome, @preco, @quantidade, @valorTotal)", connection);
                                        cmdItem.Parameters.AddWithValue("pedidoId", pedidoId);
                                        cmdItem.Parameters.AddWithValue("produtoId", item.ProdutoId);
                                        cmdItem.Parameters.AddWithValue("nome", item.NomeProduto);
                                        cmdItem.Parameters.AddWithValue("preco", item.PrecoUnitario);
                                        cmdItem.Parameters.AddWithValue("quantidade", item.Quantidade);
                                        cmdItem.Parameters.AddWithValue("valorTotal", item.ValorTotalItem);
                                        await cmdItem.ExecuteNonQueryAsync();
                                    }
                                    Console.WriteLine("Itens do Pedido:");
                                    foreach (var item in itens)
                                    {
                                        Console.WriteLine($"Produto: {item.NomeProduto} | Quantidade: {item.Quantidade} | Preço: R${item.PrecoUnitario} | Total: R${item.ValorTotalItem}");
                                    }
                                    Console.WriteLine();
                                    connection.Close();
                                }
                                else
                                {
                                    Console.WriteLine("Falha ao conectar ao banco de dados. Tente novamente.\n");
                                    connection.Close();
                                    return;
                                }
                                // Aqui você pode adicionar a lógica para cadastrar um pedido
                                break;
                            case 2:
                                Console.WriteLine("Listar Pedidos selecionado.\n");
                                Console.Write("Informe o ID do pedido: ");
                                int idPedido = Convert.ToInt32(Console.ReadLine());

                                await connection.OpenAsync();
                                if (connection.State == System.Data.ConnectionState.Open)
                                {
                                    var sql = @"SELECT p.id AS pedido_id, p.cliente, p.data_pedido, p.valor_total, i.nome_produto, i.quantidade, i.preco_unitario, i.valor_total_item FROM pedidos p JOIN itens_pedido i ON p.id = i.pedido_id WHERE p.id = @id";
                                    using var cmd = new NpgsqlCommand(sql, connection);
                                    cmd.Parameters.AddWithValue("id", idPedido);
                                    await using var reader = await cmd.ExecuteReaderAsync();
                                    if (!reader.HasRows)
                                    {
                                        Console.WriteLine("Pedido não encontrado.\n");
                                    }
                                    else
                                    {
                                        Pedido pedido = null;
                                        while (await reader.ReadAsync())
                                        {
                                            if (pedido == null)
                                            {
                                                pedido = new Pedido
                                                {
                                                    Id = reader.GetInt32(0),
                                                    Cliente = reader.GetString(1),
                                                    DataPedido = reader.GetDateTime(2),
                                                    ValorTotalPedido = reader.GetDecimal(3),
                                                    Itens = new List<ItemPedido>()
                                                };
                                            }

                                            pedido.Itens.Add(new ItemPedido
                                            {
                                                ProdutoId = reader.GetInt32(0),
                                                NomeProduto = reader.GetString(4),
                                                Quantidade = reader.GetInt32(5),
                                                PrecoUnitario = reader.GetDecimal(6),
                                                ValorTotalItem = reader.GetDecimal(7)
                                            });
                                        }

                                        if (pedido != null)
                                        {
                                            string jsonCompleto = JsonSerializer.Serialize(pedido, new JsonSerializerOptions { WriteIndented = true });
                                            Console.WriteLine(jsonCompleto);
                                            Console.WriteLine("\n");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Pedido não encontrado.\n");
                                        }
                                    }
                                    connection.Close();
                                }
                                else
                                {
                                    Console.WriteLine("Falha ao conectar ao banco de dados. Tente novamente.\n");
                                    connection.Close();
                                }
                                break;
                            case 3:
                                int opcaoAtualizarPedido = 0;
                                Console.WriteLine("O que Deseja Fazer.\n");
                                while (opcaoAtualizarPedido != 5)
                                {
                                    Console.WriteLine("1 - Atualizar Pedido");
                                    Console.WriteLine("2 - Excluir Item do Pedido");
                                    Console.WriteLine("3 - Adicionar Item ao Pedido");
                                    Console.WriteLine("4 - Editar Item do Pedido");
                                    Console.WriteLine("5 - Voltar ao Menu de Pedidos");
                                    opcaoAtualizarPedido = Convert.ToInt32(Console.ReadLine());

                                    switch (opcaoAtualizarPedido)
                                    {
                                        case 1:
                                            Console.WriteLine("Atualizar Pedido selecionado.\n");
                                            await connection.OpenAsync();
                                            if (connection.State == System.Data.ConnectionState.Open)
                                            {
                                                Console.Write("Informe o ID do pedido a ser atualizado: ");
                                                int idAtualizarPedido = Convert.ToInt32(Console.ReadLine());
                                                using (var cmd = new NpgsqlCommand("SELECT * FROM pedidos WHERE id=@id", connection))
                                                {
                                                    cmd.Parameters.AddWithValue("id", idAtualizarPedido);
                                                    await using var reader = await cmd.ExecuteReaderAsync();
                                                    if (await reader.ReadAsync())
                                                    {
                                                        Console.WriteLine($"ID: {reader.GetInt32(0)}, Cliente: {reader.GetString(1)}, Data: {reader.GetDateTime(2)}, Valor Total: {reader.GetDecimal(3)}");
                                                        reader.Close(); // Fecha o leitor antes de atualizar
                                                        Console.Write("Informe o novo nome do cliente: ");
                                                        string novoCliente = Console.ReadLine() ?? string.Empty;
                                                        Console.Write("Informe a nova data do pedido (dd/MM/yyyy): ");
                                                        DateTime novaDataPedido = DateTime.Parse(Console.ReadLine() ?? string.Empty);
                                                        using (var updateCmd = new NpgsqlCommand("UPDATE pedidos SET cliente = @cliente, data_pedido = @dataPedido WHERE id = @id", connection))
                                                        {
                                                            updateCmd.Parameters.AddWithValue("cliente", novoCliente);
                                                            updateCmd.Parameters.AddWithValue("dataPedido", novaDataPedido);
                                                            updateCmd.Parameters.AddWithValue("id", idAtualizarPedido);
                                                            await updateCmd.ExecuteNonQueryAsync();
                                                            Console.WriteLine("Pedido atualizado com sucesso!\n");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("Pedido não encontrado.\n");
                                                    }
                                                }
                                                connection.Close();
                                            }
                                            else
                                            {
                                                Console.WriteLine("Falha ao conectar ao banco de dados. Tente novamente.\n");
                                                connection.Close();
                                                return;
                                            }
                                            break;
                                        case 2:
                                            Console.WriteLine("Excluir Item do Pedido selecionado.\n");
                                            Console.Write("Informe o ID do pedido: ");
                                            int idPedidoExcluirItem = Convert.ToInt32(Console.ReadLine());
                                            await connection.OpenAsync();
                                            using (var cmd = new NpgsqlCommand("SELECT p.id AS pedido_id,ip.id AS item_id,ip.nome_produto,ip.quantidade,ip.preco_unitario,ip.valor_total_item FROM itens_pedido ip JOIN pedidos p ON ip.pedido_id = p.id WHERE p.id = @pedido_id", connection))
                                            {
                                                cmd.Parameters.AddWithValue("pedido_id", idPedidoExcluirItem);
                                                await using var reader = await cmd.ExecuteReaderAsync();
                                                bool pedidoEncontrado = false;
                                                while (await reader.ReadAsync())
                                                {
                                                    int pedidoId = Convert.ToInt32(reader["pedido_id"]);
                                                    int itemId = Convert.ToInt32(reader["item_id"]);
                                                    string nome = reader["nome_produto"] is DBNull ? "Desconhecido" : reader["nome_produto"].ToString();
                                                    int qtd = reader["quantidade"] is DBNull ? 0 : Convert.ToInt32(reader["quantidade"]);
                                                    decimal preco = reader["preco_unitario"] is DBNull ? 0 : Convert.ToDecimal(reader["preco_unitario"]);
                                                    decimal total = reader["valor_total_item"] is DBNull ? 0 : Convert.ToDecimal(reader["valor_total_item"]);
                                                    Console.WriteLine($"ID: {itemId}, ID do Item: {pedidoId}, Produto: {nome}, Quantidade: {qtd}, Preço Unitário: {preco:F2}, Valor Total: {total:F2}");
                                                    pedidoEncontrado = true;
                                                }
                                                if (pedidoEncontrado)
                                                {
                                                    reader.Close(); // Fecha o leitor antes de excluir
                                                    Console.Write("Informe o ID do item a ser excluído: ");
                                                    int idItemExcluir = Convert.ToInt32(Console.ReadLine());

                                                    using (var deleteCmd = new NpgsqlCommand("DELETE FROM itens_pedido WHERE id = @id", connection))
                                                    {
                                                        deleteCmd.Parameters.AddWithValue("id", idItemExcluir); // CORRIGIDO AQUI!
                                                        int rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                                                        if (rowsAffected > 0)
                                                        {
                                                            Console.WriteLine("Item excluído com sucesso!\n");
                                                        }
                                                        else
                                                        {
                                                            Console.WriteLine("Item não encontrado ou já excluído.\n");
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Pedido Não Existente.\n");
                                                }
                                                await connection.CloseAsync();
                                            }
                                            // Excluir Item do Pedido
                                            break;
                                        case 3:
                                            Console.WriteLine("Adicionar Item ao Pedido selecionado.\n");
                                            Console.Write("Informe o ID do pedido: ");
                                            int idPedidoAdicionarItem = Convert.ToInt32(Console.ReadLine());
                                            await connection.OpenAsync();
                                            using (var cmd = new NpgsqlCommand("SELECT * FROM pedidos WHERE id=@id", connection))
                                            {
                                                cmd.Parameters.AddWithValue("id", idPedidoAdicionarItem);
                                                await using var reader = await cmd.ExecuteReaderAsync();
                                                if (await reader.ReadAsync())
                                                {
                                                    //Console.WriteLine($"ID: {reader.GetInt32(0)}, Cliente: {reader.GetString(1)}, Data: {reader.GetDateTime(2)}, Valor Total: {reader.GetDecimal(3)}");
                                                    await reader.DisposeAsync(); // Fecha o leitor antes de adicionar
                                                    Console.Write("Informe o ID do produto a ser adicionado: ");
                                                    int produtoId = Convert.ToInt32(Console.ReadLine());
                                                    Console.Write("Informe a quantidade do produto: ");
                                                    int quantidade = Convert.ToInt32(Console.ReadLine());
                                                    // Buscar produto no banco
                                                    // Buscar produto no banco usando nova conexão
                                                    await using var novaConexao = new NpgsqlConnection(connectionString);
                                                    await novaConexao.OpenAsync();
                                                    using (var cmdProduto = new NpgsqlCommand("SELECT nome, preco FROM produtos WHERE id = @id", novaConexao))
                                                    {
                                                        cmdProduto.Parameters.AddWithValue("id", produtoId);
                                                        await using var readerProduto = await cmdProduto.ExecuteReaderAsync();
                                                        if (!await readerProduto.ReadAsync())
                                                        {
                                                            Console.WriteLine("Produto não encontrado!");
                                                            await readerProduto.DisposeAsync();
                                                            await novaConexao.CloseAsync();
                                                            break;
                                                        }
                                                        string nomeProduto = readerProduto.GetString(0);
                                                        decimal precoUnitario = readerProduto.GetDecimal(1);
                                                        decimal valorTotalItem = precoUnitario * quantidade;
                                                        await readerProduto.DisposeAsync();
                                                        await novaConexao.CloseAsync();
                                                        // Inserir item no pedido (de volta na conexão principal)
                                                        using (var insertCmd = new NpgsqlCommand("INSERT INTO itens_pedido (pedido_id, produto_id, nome_produto, preco_unitario, quantidade, valor_total_item) VALUES (@pedidoId, @produtoId, @nome, @preco, @quantidade, @valorTotal)", connection))
                                                        {
                                                            insertCmd.Parameters.AddWithValue("pedidoId", idPedidoAdicionarItem);
                                                            insertCmd.Parameters.AddWithValue("produtoId", produtoId);
                                                            insertCmd.Parameters.AddWithValue("nome", nomeProduto);
                                                            insertCmd.Parameters.AddWithValue("preco", precoUnitario);
                                                            insertCmd.Parameters.AddWithValue("quantidade", quantidade);
                                                            insertCmd.Parameters.AddWithValue("valorTotal", valorTotalItem);
                                                            await insertCmd.ExecuteNonQueryAsync();
                                                            Console.WriteLine("Item adicionado ao pedido com sucesso!\n");
                                                        }
                                                    }
                                                    await connection.CloseAsync();
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Pedido não encontrado.\n");
                                                    await connection.CloseAsync();
                                                }
                                            }
                                            // Adicionar Item ao Pedido
                                            break;
                                        case 4:
                                            Console.WriteLine("Editar Item do Pedido selecionado.\n");
                                            Console.Write("Informe o ID do pedido: ");
                                            int idPedidoEditarItem = Convert.ToInt32(Console.ReadLine());
                                            await connection.OpenAsync();
                                            using (var cmd = new NpgsqlCommand(@"SELECT ip.id AS item_id, ip.nome_produto, ip.quantidade, ip.preco_unitario, ip.valor_total_item FROM itens_pedido ip JOIN pedidos p ON ip.pedido_id = p.id WHERE p.id = @pedido_id", connection))
                                            {
                                                cmd.Parameters.AddWithValue("pedido_id", idPedidoEditarItem);
                                                await using var reader = await cmd.ExecuteReaderAsync();
                                                bool pedidoEncontrado = false;
                                                while (await reader.ReadAsync())
                                                {
                                                    int itemId = Convert.ToInt32(reader["item_id"]);
                                                    string nome = reader["nome_produto"].ToString();
                                                    int qtd = Convert.ToInt32(reader["quantidade"]);
                                                    decimal preco = Convert.ToDecimal(reader["preco_unitario"]);
                                                    decimal total = Convert.ToDecimal(reader["valor_total_item"]);
                                                    Console.WriteLine($"Item ID: {itemId}, Produto: {nome}, Quantidade: {qtd}, Preço Unitário: {preco:F2}, Total: {total:F2}");
                                                    pedidoEncontrado = true;
                                                }
                                                reader.Close();
                                                if (!pedidoEncontrado)
                                                {
                                                    Console.WriteLine("Pedido não encontrado ou sem itens.");
                                                    await connection.CloseAsync();
                                                    break;
                                                }
                                                Console.Write("Informe o ID do item a ser editado: ");
                                                int idItemEditar = Convert.ToInt32(Console.ReadLine());
                                                Console.Write("Informe a nova quantidade: ");
                                                int novaQuantidade = Convert.ToInt32(Console.ReadLine());
                                                // Atualizar o item com base no ID único
                                                using (var updateCmd = new NpgsqlCommand(@"UPDATE itens_pedido SET quantidade = @quantidade, valor_total_item = preco_unitario * @quantidade WHERE id = @id", connection))
                                                {
                                                    updateCmd.Parameters.AddWithValue("quantidade", novaQuantidade);
                                                    updateCmd.Parameters.AddWithValue("id", idItemEditar);
                                                    int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                                                    if (rowsAffected > 0)
                                                    {
                                                        Console.WriteLine("Item do pedido atualizado com sucesso!\n");
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("Item não encontrado ou erro ao atualizar.\n");
                                                    }
                                                }
                                                await connection.CloseAsync();
                                            }
                                            break;
                                        case 5:
                                            Console.WriteLine("Voltando ao Menu de Pedidos...\n");
                                            break;
                                        default:
                                            Console.WriteLine("Opcao invalida, tente novamente.\n");
                                            break;
                                    }
                                }
                                break;
                            case 4:
                                Console.WriteLine("Excluir Pedido selecionado.\n");
                                await connection.OpenAsync();
                                if (connection.State == System.Data.ConnectionState.Open)
                                {
                                    Console.Write("Informe o ID do pedido a ser excluído: ");
                                    int idExcluirPedido = Convert.ToInt32(Console.ReadLine());
                                    using (var cmd = new NpgsqlCommand("DELETE FROM pedidos WHERE id = @id", connection))
                                    {
                                        cmd.Parameters.AddWithValue("id", idExcluirPedido);
                                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                                        if (rowsAffected > 0)
                                        {
                                            Console.WriteLine("Pedido excluído com sucesso!\n");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Pedido não encontrado ou já excluído.\n");
                                        }
                                    }
                                    connection.Close();
                                }
                                else
                                {
                                    Console.WriteLine("Falha ao conectar ao banco de dados. Tente novamente.\n");
                                    connection.Close();
                                    return;
                                }
                                break;
                            case 5:
                                Console.WriteLine("Voltando ao Menu Principal...\n");
                                break;
                            default:
                                Console.WriteLine("Opcao invalida, tente novamente.\n");
                                break;
                        }
                    }
                    break;
                case 3:
                    Console.WriteLine("Voce escolheu Sair do Sistema de Estoque, Produtos e Pedidos.");
                    Console.WriteLine("Obrigado por usar o nosso Sistema!");
                    Console.WriteLine("Ate logo!\n");
                    return;
                default:
                    Console.WriteLine("Opcao invalida, tente novamente.\n");
                    break;
            }
        }
    }
    public class Produto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal Preco { get; set; }
        public int QuantidadeEstoque { get; set; }
    }
    public class Pedido
    {
        public int Id { get; set; }
        public string Cliente { get; set; }
        public List<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
        public decimal ValorTotalPedido { get; set; }
        public DateTime DataPedido { get; set; }
    }
    public class ItemPedido
    {
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal ValorTotalItem { get; set; }
    }
}