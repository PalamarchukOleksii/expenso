namespace ExpensoServer.Common.Constants;

public static class EndpointRoutes
{
    public const string Prefix = "api";

    public static class Segments
    {
        public const string Auth = "auth";
        public const string Users = "users";
        public const string Accounts = "accounts";
        public const string IncomeCategories = "income-categories";
        public const string ExpenseCategories = "expense-categories";
        public const string IncomeOperations = "income-operations";
        public const string ExpenseOperations = "expense-operations";
        public const string TransferOperations = "transfer-operations";
        public const string ExchangeRates = "exchange-rates";
    }
}