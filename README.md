yay, another dependency injection library :)

TODO:

- make IEnumerable lazy. motivation is that there are a lot of ExportFactory importing common services but pre creating
  them (like around 50 instances) every time is expensive
- think of more tests that uses lib as mef replacement kinda
- move LazyHelper to IL generation
- implement IExporter that exports to binary-format-like files and implement loading from that
- cache ALL results of reflection calls (Type.GetType etc.) binding IS EXPENSIVE!!!

## License

This project is licensed under the GNU Lesser General Public License v3.0 only (LGPL-3.0-only).

See [COPYING](./COPYING) and [COPYING.LESSER](./COPYING.LESSER) for the full license texts.
