namespace BPInventoryOps.Api.Pages.Shared;

public sealed record Pagination(int Page, int TotalPages, int TotalCount, string PageKey = "Query.Page");
