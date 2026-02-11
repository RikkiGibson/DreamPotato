import * as vscode from 'vscode';

export class Lc86kDocumentSymbolProvider implements vscode.DocumentSymbolProvider {
    provideDocumentSymbols(
        document: vscode.TextDocument,
        token: vscode.CancellationToken
    ): vscode.DocumentSymbol[] {
        const symbols: vscode.DocumentSymbol[] = [];
        let currentGlobalLabel: vscode.DocumentSymbol | null = null;

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

                // End the current global label range and start a new global label.
                if (currentGlobalLabel) {
                    currentGlobalLabel.range = new vscode.Range(
                        currentGlobalLabel.range.start,
                        range.start
                    );
                }
                currentGlobalLabel = new vscode.DocumentSymbol(
                    name,
                    '',
                    vscode.SymbolKind.Function,
                    range,
                    selectionRange
                );
                symbols.push(currentGlobalLabel);
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

                if (currentGlobalLabel) {
                    currentGlobalLabel.children.push(localSymbol);
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

        if (currentGlobalLabel) {
            let lastLine = document.lineAt(document.lineCount - 1);
            currentGlobalLabel.range = new vscode.Range(
                currentGlobalLabel.range.start,
                lastLine.range.end
            );
        }

        return symbols;
    }
}
