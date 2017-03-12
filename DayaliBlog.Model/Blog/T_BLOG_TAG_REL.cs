﻿using System;
namespace DayaliBlog.Model.Blog
{
	/// <summary>
	/// T_BLOG_TAG_REL:实体类(属性说明自动提取数据库字段的描述信息)
	/// </summary>
	public partial class T_BLOG_TAG_REL
	{
		public T_BLOG_TAG_REL()
		{}
		#region Model
		private int _blogid;
		private int _tagid;
		/// <summary>
		/// 
		/// </summary>
		public int BlogID
		{
			set{ _blogid=value;}
			get{return _blogid;}
		}
		/// <summary>
		/// 
		/// </summary>
		public int TagID
		{
			set{ _tagid=value;}
			get{return _tagid;}
		}
		#endregion Model

	}
}

