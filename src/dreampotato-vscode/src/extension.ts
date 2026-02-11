import * as vscode from 'vscode';
import { Lc86kDocumentSymbolProvider } from './symbolProvider';

export function activate(context: vscode.ExtensionContext) {
    const symbolProvider = new Lc86kDocumentSymbolProvider();
    context.subscriptions.push(
        vscode.languages.registerDocumentSymbolProvider(
            { language: 'lc86k' },
            symbolProvider
        )
    );
}

export function deactivate() {}
