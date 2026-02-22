# Notes on stack data

- Many built in operations like PUSH/POP/CALL/RET operate on the stack.
- But to accurately track "what is going on in the stack", we need to know *any* ops that are modifying the stack space.
- However we may be better off with a simpler solution which is 90% accurate.

Stack display should accurately reflect machine state.
So a stack entry needs the following:
- Kind
- Source (e.g. direct address of PUSH/POP. Note there is only 'd9' mode for these, no immediate or indirect)
- Value (e.g. an 8-bit data value or a 16-bit address)
- Offset (the actual address in stack memory we are dealing with).

We should detect if user directly writes to stack data space.  This should potentially be considered memory corruption. At least, affected entries in our list of StackData, should be adjusted appropriately. e.g. replace the affected entry with whatever we observe/read was written to the stack memory area and mark it as "we don't know what this is".

For the first pass we will definitely just let things desync if user directly manipulates stack memory. As a stretch goal will log errors/warnings if the stack pointer, stack memory etc are "directly" manipulated outside of push/pop/call/ret.

---

Display of stack frames
- It feels like for calls, the instr that should be highlighted/displayed should be the particular CALL instruction. We can just do this, it feels straightforward.
- For interrupts, it's tempting to highlight "what we were doing when the interrupt happened". `SET1 PCON,0` is the big example. But in general, we need to take care that when an instr is highlighted, it displays a task which is "not done yet". e.g. for the `set1 ie,7; inc flag; inc flag; inc flag;`, the stack frame *must* point at an 'inc' whose effects haven't been observed yet. This is a tricky needle to thread.
