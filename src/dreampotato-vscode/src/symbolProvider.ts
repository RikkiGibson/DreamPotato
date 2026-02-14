import * as vscode from 'vscode';


type AsmKind = "globalLabel" | "localLabel" | "const";
class AsmSymbol extends vscode.DocumentSymbol {
    asmKind: AsmKind;

    constructor(asmKind: AsmKind, name: string, detail: string, kind: vscode.SymbolKind, range: vscode.Range, selectionRange: vscode.Range) {
        super(name, detail, kind, range, selectionRange);
        this.asmKind = asmKind;
    }
}

export class Lc86kDocumentSymbolProvider implements vscode.DocumentSymbolProvider {
    provideDocumentSymbols(
        document: vscode.TextDocument,
        token: vscode.CancellationToken
    ): vscode.DocumentSymbol[] {
        const asmSymbols = this.findAsmSymbols(document, token);
        this.setRanges(document, asmSymbols);
        return asmSymbols;
    }

    findAsmSymbols(
        document: vscode.TextDocument,
        token: vscode.CancellationToken
    ): AsmSymbol[] {
        const symbols: AsmSymbol[] = [];

        let currentGlobalSymbol: AsmSymbol | undefined;
        for (let i = 0; i < document.lineCount; i++) {
            if (token.isCancellationRequested) {
                return symbols;
            }

            const line = document.lineAt(i);
            const text = line.text;

            function assertHasValue(value: number | undefined): number {
                if (value == null) {
                    throw new Error();
                }

                return value;
            }

            function eatLabel(kind: "globalLabel" | "localLabel", match: RegExpMatchArray): AsmSymbol {
                var startCharacter = assertHasValue(match.index);
                const range = new vscode.Range(
                    i, startCharacter,
                    i, startCharacter + match[0].length
                );
                const selectionRange = new vscode.Range(
                    i, startCharacter,
                    i, startCharacter // note: this is adjusted in a later step.
                );
                const name = match[1];

                const detail = ''; // TODO: search upward for comments
                return new AsmSymbol(
                    kind,
                    name,
                    detail,
                    vscode.SymbolKind.Function,
                    range,
                    selectionRange
                );
            }

            const globalMatch = text.match(/^([a-zA-Z_][a-zA-Z0-9_]*)\s*:/);
            if (globalMatch) {
                currentGlobalSymbol = eatLabel("globalLabel", globalMatch);
                symbols.push(currentGlobalSymbol);
                continue;
            }

            const localMatch = text.match(/^(\.[a-zA-Z_][a-zA-Z0-9_]*)\s*:/);
            if (localMatch) {
                const localLabel = eatLabel("localLabel", localMatch);
                if (currentGlobalSymbol) {
                    currentGlobalSymbol.children.push(localLabel);
                } else {
                    symbols.push(localLabel);
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

                symbols.push(new AsmSymbol(
                    "const",
                    name,
                    value,
                    vscode.SymbolKind.Constant,
                    range,
                    selectionRange
                ));

                continue;
            }
        }

        return symbols;
    }

    setRanges(document: vscode.TextDocument, asmSymbols: AsmSymbol[]) {
        const documentEndPos = document.lineAt(document.lineCount - 1).range.end;
        for (let i = 0; i < asmSymbols.length; i++) {
            const symbol = asmSymbols[i];
            if (symbol.asmKind == "globalLabel") {
                setLabelRange(asmSymbols, i, documentEndPos);
                for (let j = 0; j < symbol.children.length; j++) {
                    setLabelRange(symbol.children as AsmSymbol[], j, symbol.range.end);
                }
            }
            else if (symbol.asmKind == "localLabel") {
                setLabelRange(asmSymbols, i, documentEndPos);
            }
            else if (symbol.asmKind == "const") {
                continue;
            }
            else {
                throw new Error();
            }
        }

        function setLabelRange(siblingSymbols: AsmSymbol[], index: number, containerEndPos: vscode.Position) {
            const symbol = siblingSymbols[index];
            if (index === siblingSymbols.length - 1) {
                symbol.range = new vscode.Range(symbol.range.start, containerEndPos);
                return;
            }

            const nextSibling = siblingSymbols[index + 1];
            const endTextLine = document.lineAt(nextSibling.range.end.line - 1);
            symbol.range = new vscode.Range(symbol.range.start, endTextLine.range.end);
        }
    }
}
