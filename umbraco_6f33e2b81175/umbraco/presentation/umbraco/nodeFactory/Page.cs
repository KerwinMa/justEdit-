using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.propertytype;
using System.Collections.Generic;
using umbraco.interfaces;

namespace umbraco.NodeFactory
{
    /// <summary>
    /// Summary description for Node.
    /// </summary>

    [Serializable]
    [XmlType(Namespace = "http://umbraco.org/webservices/")]
    public class Node : INode
    {
        private Hashtable _aliasToNames = new Hashtable();

        private bool _initialized = false;
        private Nodes _children = new Nodes();
        private Node _parent = null;
        private int _id;
        private int _template;
        private string _name;
        private string _nodeTypeAlias;
        private string _writerName;
        private string _creatorName;
        private int _writerID;
        private int _creatorID;
        private string _urlName;
        private string _path;
        private DateTime _createDate;
        private DateTime _updateDate;
        private Guid _version;
        private Properties _properties = new Properties();
        private XmlNode _pageXmlNode;
        private int _sortOrder;
        private int _level;

        public Nodes Children
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _children;
            }
        }

        public INode Parent
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _parent;
            }
        }

        public int Id
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _id;
            }
        }

        public int template
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _template;
            }
        }

        public int SortOrder
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _sortOrder;
            }
        }

        public string Name
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _name;
            }
        }

        public string Url
        {
            get { return library.NiceUrl(Id); }
        }

        public string NodeTypeAlias
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _nodeTypeAlias;
            }
        }

        public string WriterName
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _writerName;
            }
        }

        public string CreatorName
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _creatorName;
            }
        }

        public int WriterID
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _writerID;
            }
        }

        public int CreatorID
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _creatorID;
            }
        }


        public string Path
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _path;
            }
        }

        public DateTime CreateDate
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _createDate;
            }
        }

        public DateTime UpdateDate
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _updateDate;
            }
        }

        public Guid Version
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _version;
            }
        }

        public string UrlName
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _urlName;
            }
        }

        public string NiceUrl
        {
            get
            {
                return library.NiceUrl(_id);
            }
        }

        public int Level
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _level;
            }
        }

        public List<IProperty> PropertiesAsList
        {
            get { return Properties.Cast<IProperty>().ToList(); }
        }

        public Properties Properties
        {
            get
            {
                if (!_initialized)
                    initialize();
                return _properties;
            }
        }

        public Node()
        {
            _pageXmlNode = ((IHasXmlNode)library.GetXmlNodeCurrent().Current).GetNode();
            initializeStructure();
            initialize();
        }

        public Node(XmlNode NodeXmlNode)
        {
            _pageXmlNode = NodeXmlNode;
            initializeStructure();
            initialize();
        }

        public Node(XmlNode NodeXmlNode, bool DisableInitializing)
        {
            _pageXmlNode = NodeXmlNode;
            initializeStructure();
            if (!DisableInitializing)
                initialize();
        }

        /// <summary>
        /// Special constructor for by-passing published vs. preview xml to use
        /// when updating the SiteMapProvider
        /// </summary>
        /// <param name="NodeId"></param>
        /// <param name="forcePublishedXml"></param>
        public Node(int NodeId, bool forcePublishedXml)
        {
            if (forcePublishedXml)
            {
                if (NodeId != -1)
                    _pageXmlNode = content.Instance.XmlContent.GetElementById(NodeId.ToString());
                else
                {
                    _pageXmlNode = content.Instance.XmlContent.DocumentElement;

                }
                initializeStructure();
                initialize();
            }
            else
            {
                throw new ArgumentException("Use Node(int NodeId) if not forcing published xml");

            }
        }

        public Node(int NodeId)
        {
            if (NodeId != -1)
                _pageXmlNode = ((IHasXmlNode)library.GetXmlNodeById(NodeId.ToString()).Current).GetNode();
            else
            {
                _pageXmlNode = umbraco.presentation.UmbracoContext.Current.GetXml().DocumentElement;

            }
            initializeStructure();
            initialize();
        }

        public IProperty GetProperty(string Alias)
        {
            foreach (Property p in Properties)
            {
                if (p.Alias == Alias)
                    return p;
            }
            return null;
        }

        public IProperty GetProperty(string Alias, out bool propertyExists)
        {
            foreach (Property p in Properties)
            {
                if (p.Alias == Alias)
                {
                    propertyExists = true;
                    return p;
                }
            }
            propertyExists = false;
            return null;
        }

        public static Node GetNodeByXpath(string xpath)
        {
            XPathNodeIterator itNode = library.GetXmlNodeByXPath(xpath);
            XmlNode nodeXmlNode = null;
            if (itNode.MoveNext())
            {
                nodeXmlNode = ((IHasXmlNode)itNode.Current).GetNode();
            }
            if (nodeXmlNode != null)
            {
                return new Node(nodeXmlNode);
            }
            return null;
        }

        public List<INode> ChildrenAsList
        {
            get { return Children.Cast<INode>().ToList(); }
        }

        public DataTable ChildrenAsTable()
        {
            if (Children.Count > 0)
            {
                DataTable dt = generateDataTable(Children[0]);

                string firstNodeTypeAlias = Children[0].NodeTypeAlias;

                foreach (Node n in Children)
                {
                    if (n.NodeTypeAlias == firstNodeTypeAlias)
                    {
                        DataRow dr = dt.NewRow();
                        populateRow(ref dr, n, getPropertyHeaders(n));
                        dt.Rows.Add(dr);
                    }
                }
                return dt;
            }
            else
                return new DataTable();
        }

        public DataTable ChildrenAsTable(string nodeTypeAliasFilter)
        {

            if (Children.Count > 0)
            {

                Node Firstnode = null;
                Boolean nodeFound = false;
                foreach (Node n in Children)
                {
                    if (n.NodeTypeAlias == nodeTypeAliasFilter && !nodeFound)
                    {
                        Firstnode = n;
                        nodeFound = true;
                        break;
                    }
                }

                if (nodeFound)
                {
                    DataTable dt = generateDataTable(Firstnode);

                    foreach (Node n in Children)
                    {
                        if (n.NodeTypeAlias == nodeTypeAliasFilter)
                        {
                            DataRow dr = dt.NewRow();
                            populateRow(ref dr, n, getPropertyHeaders(n));
                            dt.Rows.Add(dr);
                        }
                    }
                    return dt;
                }
                else
                {
                    return new DataTable();
                }
            }
            else
                return new DataTable();
        }

        private DataTable generateDataTable(Node SchemaNode)
        {
            DataTable NodeAsDataTable = new DataTable(SchemaNode.NodeTypeAlias);
            string[] defaultColumns = {
                                          "Id", "NodeName", "NodeTypeAlias", "CreateDate", "UpdateDate", "CreatorName",
                                          "WriterName", "Url"
                                      };
            foreach (string s in defaultColumns)
            {
                DataColumn dc = new DataColumn(s);
                NodeAsDataTable.Columns.Add(dc);
            }

            // add properties
            Hashtable propertyHeaders = getPropertyHeaders(SchemaNode);
            IDictionaryEnumerator ide = propertyHeaders.GetEnumerator();
            while (ide.MoveNext())
            {
                DataColumn dc = new DataColumn(ide.Value.ToString());
                NodeAsDataTable.Columns.Add(dc);
            }

            return NodeAsDataTable;
        }

        private Hashtable getPropertyHeaders(Node SchemaNode)
        {
            if (_aliasToNames.ContainsKey(SchemaNode.NodeTypeAlias))
                return (Hashtable)_aliasToNames[SchemaNode.NodeTypeAlias];
            else
            {
                ContentType ct = ContentType.GetByAlias(SchemaNode.NodeTypeAlias);
                Hashtable def = new Hashtable();
                foreach (PropertyType pt in ct.PropertyTypes)
                    def.Add(pt.Alias, pt.Name);
                System.Web.HttpContext.Current.Application.Lock();
                _aliasToNames.Add(SchemaNode.NodeTypeAlias, def);
                System.Web.HttpContext.Current.Application.UnLock();

                return def;
            }
        }

        private void populateRow(ref DataRow dr, Node n, Hashtable AliasesToNames)
        {
            dr["Id"] = n.Id;
            dr["NodeName"] = n.Name;
            dr["NodeTypeAlias"] = n.NodeTypeAlias;
            dr["CreateDate"] = n.CreateDate;
            dr["UpdateDate"] = n.UpdateDate;
            dr["CreatorName"] = n.CreatorName;
            dr["WriterName"] = n.WriterName;
            dr["Url"] = library.NiceUrl(n.Id);

            int counter = 8;
            foreach (Property p in n.Properties)
            {
                if (p.Value != null)
                {
                    dr[AliasesToNames[p.Alias].ToString()] = p.Value;
                    counter++;
                }
            }
        }


        private void initializeStructure()
        {
            // Load parent if it exists and is a node

            if (_pageXmlNode != null && _pageXmlNode.SelectSingleNode("..") != null)
            {
                XmlNode parent = _pageXmlNode.SelectSingleNode("..");
                if (parent != null && (parent.Name == "node" || (parent.Attributes != null && parent.Attributes.GetNamedItem("isDoc") != null)))
                    _parent = new Node(parent, true);
            }
        }

        private void initialize()
        {
            if (_pageXmlNode != null)
            {
                _initialized = true;
                if (_pageXmlNode.Attributes != null)
                {
                    _id = int.Parse(_pageXmlNode.Attributes.GetNamedItem("id").Value);
                    if (_pageXmlNode.Attributes.GetNamedItem("template") != null)
                        _template = int.Parse(_pageXmlNode.Attributes.GetNamedItem("template").Value);
                    if (_pageXmlNode.Attributes.GetNamedItem("sortOrder") != null)
                        _sortOrder = int.Parse(_pageXmlNode.Attributes.GetNamedItem("sortOrder").Value);
                    if (_pageXmlNode.Attributes.GetNamedItem("nodeName") != null)
                        _name = _pageXmlNode.Attributes.GetNamedItem("nodeName").Value;
                    if (_pageXmlNode.Attributes.GetNamedItem("writerName") != null)
                        _writerName = _pageXmlNode.Attributes.GetNamedItem("writerName").Value;
                    if (_pageXmlNode.Attributes.GetNamedItem("urlName") != null)
                        _urlName = _pageXmlNode.Attributes.GetNamedItem("urlName").Value;
                    // Creatorname is new in 2.1, so published xml might not have it!
                    try
                    {
                        _creatorName = _pageXmlNode.Attributes.GetNamedItem("creatorName").Value;
                    }
                    catch
                    {
                        _creatorName = _writerName;
                    }

                    //Added the actual userID, as a user cannot be looked up via full name only... 
                    if (_pageXmlNode.Attributes.GetNamedItem("creatorID") != null)
                        _creatorID = int.Parse(_pageXmlNode.Attributes.GetNamedItem("creatorID").Value);
                    if (_pageXmlNode.Attributes.GetNamedItem("writerID") != null)
                        _writerID = int.Parse(_pageXmlNode.Attributes.GetNamedItem("writerID").Value);

                    if (UmbracoSettings.UseLegacyXmlSchema)
                    {
                        if (_pageXmlNode.Attributes.GetNamedItem("nodeTypeAlias") != null)
                            _nodeTypeAlias = _pageXmlNode.Attributes.GetNamedItem("nodeTypeAlias").Value;
                    }
                    else
                    {
                        _nodeTypeAlias = _pageXmlNode.Name;
                    }

                    if (_pageXmlNode.Attributes.GetNamedItem("path") != null)
                        _path = _pageXmlNode.Attributes.GetNamedItem("path").Value;
                    if (_pageXmlNode.Attributes.GetNamedItem("version") != null)
                        _version = new Guid(_pageXmlNode.Attributes.GetNamedItem("version").Value);
                    if (_pageXmlNode.Attributes.GetNamedItem("createDate") != null)
                        _createDate = DateTime.Parse(_pageXmlNode.Attributes.GetNamedItem("createDate").Value);
                    if (_pageXmlNode.Attributes.GetNamedItem("updateDate") != null)
                        _updateDate = DateTime.Parse(_pageXmlNode.Attributes.GetNamedItem("updateDate").Value);
                    if (_pageXmlNode.Attributes.GetNamedItem("level") != null)
                        _level = int.Parse(_pageXmlNode.Attributes.GetNamedItem("level").Value);

                }

                // load data
                string dataXPath = UmbracoSettings.UseLegacyXmlSchema ? "data" : "* [not(@isDoc)]";
                foreach (XmlNode n in _pageXmlNode.SelectNodes(dataXPath))
                    _properties.Add(new Property(n));

                // load children
                string childXPath = UmbracoSettings.UseLegacyXmlSchema ? "node" : "* [@isDoc]";
                XPathNavigator nav = _pageXmlNode.CreateNavigator();
                XPathExpression expr = nav.Compile(childXPath);
                expr.AddSort("@sortOrder", XmlSortOrder.Ascending, XmlCaseOrder.None, "", XmlDataType.Number);
                XPathNodeIterator iterator = nav.Select(expr);
                while (iterator.MoveNext())
                {
                    _children.Add(
                        new Node(((IHasXmlNode)iterator.Current).GetNode(), true)
                        );
                }
            }
//            else
//                throw new ArgumentNullException("Node xml source is null");
        }

        public static Node GetCurrent()
        {
            int id = getCurrentNodeId();
            return new Node(id);
        }

        public static int getCurrentNodeId()
        {
            XmlNode n = ((IHasXmlNode)library.GetXmlNodeCurrent().Current).GetNode();
            if (n.Attributes == null || n.Attributes.GetNamedItem("id") == null)
                throw new ArgumentException("Current node is null. This might be due to previewing an unpublished node. As the NodeFactory works with published data, macros using the node factory won't work in preview mode.", "Current node is " + System.Web.HttpContext.Current.Items["pageID"].ToString());

            return int.Parse(n.Attributes.GetNamedItem("id").Value);
        } 
    }

    public class Nodes : CollectionBase
    {
        public virtual void Add(Node NewNode)
        {
            List.Add(NewNode);
        }

        public virtual Node this[int Index]
        {
            get { return (Node)List[Index]; }
        }
    }

    [Serializable]
    [XmlType(Namespace = "http://umbraco.org/webservices/")]
    public class Property : IProperty
    {
        private Guid _version;
        private string _alias;
        private string _value;

        public string Alias
        {
            get { return _alias; }
        }

        public string Value
        {
            get { return IO.IOHelper.ResolveUrlsFromTextString(_value); }
        }

        public Guid Version
        {
            get { return _version; }
        }

        public Property()
        {

        }

        public override string ToString()
        {
            return Value;
        }

        public Property(XmlNode PropertyXmlData)
        {
            if (PropertyXmlData != null)
            {
                // For backward compatibility with 2.x (the version attribute has been removed from 3.0 data nodes)
                if (PropertyXmlData.Attributes.GetNamedItem("versionID") != null)
                    _version = new Guid(PropertyXmlData.Attributes.GetNamedItem("versionID").Value);
                _alias = UmbracoSettings.UseLegacyXmlSchema ?
                    PropertyXmlData.Attributes.GetNamedItem("alias").Value :
                    PropertyXmlData.Name;
                _value = xmlHelper.GetNodeValue(PropertyXmlData);
            }
            else
                throw new ArgumentNullException("Property xml source is null");
        }
    }

    public class Properties : CollectionBase
    {
        public virtual void Add(Property NewProperty)
        {
            List.Add(NewProperty);
        }

        public virtual Property this[int Index]
        {
            get { return (Property)List[Index]; }
        }

        public virtual Property this[string alias]
        {
            get
            {
                return List.OfType<Property>()
                    .Where(x => x.Alias.Equals(alias, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();
            }
        }
    }
}