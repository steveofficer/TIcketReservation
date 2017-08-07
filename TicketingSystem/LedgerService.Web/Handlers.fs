module LedgerService.Handlers

open Suave
open Suave.Successful
open Suave.RequestErrors
open Suave.Operators
open LedgerService.Types
open Newtonsoft.Json

let ``get user cancellable tickets`` (query : string -> Async<CancellableTicket[] * System.DateTime>) (``user id`` : string) (ctx : HttpContext) = async {
    let! userAllocations = query ``user id``
    return! OK "" ctx
}