import * as vscode from 'vscode';

export class Lc86kDocumentSymbolProvider implements vscode.DocumentSymbolProvider {
    provideDocumentSymbols(
        document: vscode.TextDocument,
        token: vscode.CancellationToken
    ): vscode.DocumentSymbol[] {
        const symbols: vscode.DocumentSymbol[] = [];
        let currentGlobalSymbol: vscode.DocumentSymbol | null = null;

        for (let i = 0; i < document.lineCount; i++) {
            if (token.isCancellationRequested) {
                return symbols;
            }

            const line = document.lineAt(i);
            const text = line.text;

            const globalMatch = text.match(/^([a-zA-Z_][a-zA-Z0-9_]*)\s*:/);
            if (globalMatch) {
                const name = globalMatch[1];
                const range = new vscode.Range(
                    i, 0,
                    i, line.text.length
                );
                const selectionRange = new vscode.Range(
                    i, 0,
                    i, name.length
                );

                if (currentGlobalSymbol) {
                    currentGlobalSymbol.range = new vscode.Range(
                        currentGlobalSymbol.range.start,
                        range.end
                    );
                }
                currentGlobalSymbol = new vscode.DocumentSymbol(
                    name,
                    '',
                    vscode.SymbolKind.Function,
                    range,
                    selectionRange
                );
                symbols.push(currentGlobalSymbol);
                continue;
            }

            const localMatch = text.match(/^(\.[a-zA-Z_][a-zA-Z0-9_]*)\s*:/);
            if (localMatch) {
                const name = localMatch[1];
                const range = new vscode.Range(
                    i, 0,
                    i, line.text.length
                );
                const selectionRange = new vscode.Range(
                    i, 0,
                    i, name.length
                );

                const localSymbol = new vscode.DocumentSymbol(
                    name,
                    '',
                    vscode.SymbolKind.Method,
                    range,
                    selectionRange
                );

                if (currentGlobalSymbol) {
                    currentGlobalSymbol.children.push(localSymbol);
                } else {
                    symbols.push(localSymbol);
                }

                continue;
            }

            const constMatch = text.match(/^([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(.+)/);
            if (constMatch) {
                const name = constMatch[1];
                const value = constMatch[2].trim();
                const range = new vscode.Range(
                    i, 0,
                    i, line.text.length
                );
                const selectionRange = new vscode.Range(
                    i, 0,
                    i, name.length
                );

                const constSymbol = new vscode.DocumentSymbol(
                    name,
                    value,
                    vscode.SymbolKind.Constant,
                    range,
                    selectionRange
                );
                symbols.push(constSymbol);

                continue;
            }
        }

        if (currentGlobalSymbol) {
            let lastLine = document.lineAt(document.lineCount - 1);
            currentGlobalSymbol.range = new vscode.Range(
                currentGlobalSymbol.range.start,
                lastLine.range.end
            );
        }

        return symbols;
    }
}
